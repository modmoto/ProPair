using MongoDB.Driver;
using ProPair.Adapters.Mongo.Documents;
using ProPair.Application.Ports;
using ProPair.Domain.Common;
using ProPair.Domain.Matrices;
using ProPair.Domain.Pairing;
using ProPair.Domain.Preparations;
using ProPair.Domain.Teams;
using ProPair.Domain.Tournaments;

namespace ProPair.Adapters.Mongo;

internal static class VersionedSave
{
    /// <summary>Insert (version 0) or replace only if the stored version is unchanged. Returns the new version.</summary>
    public static async Task<long> SaveAsync<TDoc>(IMongoCollection<TDoc> collection, TDoc doc, CancellationToken ct)
        where TDoc : IVersionedDocument
    {
        var expected = doc.Version;
        doc.Version = expected + 1;
        if (expected == 0)
        {
            try { await collection.InsertOneAsync(doc, cancellationToken: ct); }
            catch (MongoWriteException e) when (e.WriteError.Category == ServerErrorCategory.DuplicateKey)
            { throw new ConcurrencyException(); }
            return doc.Version;
        }

        var filter = Builders<TDoc>.Filter.Eq(d => d.Id, doc.Id) & Builders<TDoc>.Filter.Eq(d => d.Version, expected);
        var result = await collection.ReplaceOneAsync(filter, doc, cancellationToken: ct);
        if (result.MatchedCount == 0) throw new ConcurrencyException();
        return doc.Version;
    }
}

internal sealed class TeamRepository(MongoContext db) : ITeamRepository
{
    public async Task<Team?> GetAsync(TeamId id, CancellationToken ct = default) =>
        await db.Teams.Find(d => d.Id == id.Value).FirstOrDefaultAsync(ct) is { } d ? Mapping.ToDomain(d) : null;

    public async Task<Team?> FindByNameAsync(string name, CancellationToken ct = default) =>
        await db.Teams.Find(d => d.Name == name).FirstOrDefaultAsync(ct) is { } d ? Mapping.ToDomain(d) : null;

    public async Task SaveAsync(Team team, CancellationToken ct = default) =>
        team.SetPersistedVersion(await VersionedSave.SaveAsync(db.Teams, Mapping.ToDoc(team), ct));
}

internal sealed class TournamentRepository(MongoContext db) : ITournamentRepository
{
    public async Task<Tournament?> GetAsync(TournamentId id, CancellationToken ct = default) =>
        await db.Tournaments.Find(d => d.Id == id.Value).FirstOrDefaultAsync(ct) is { } d ? Mapping.ToDomain(d) : null;

    public async Task<Tournament?> FindByExternalIdAsync(string externalId, CancellationToken ct = default) =>
        await db.Tournaments.Find(d => d.ExternalId == externalId).FirstOrDefaultAsync(ct) is { } d
            ? Mapping.ToDomain(d) : null;

    public async Task SaveAsync(Tournament tournament, CancellationToken ct = default) =>
        tournament.SetPersistedVersion(await VersionedSave.SaveAsync(db.Tournaments, Mapping.ToDoc(tournament), ct));
}

internal sealed class PreparationRepository(MongoContext db) : IPreparationRepository
{
    public async Task<Preparation?> FindByTokenAsync(string token, CancellationToken ct = default) =>
        await db.Preparations.Find(d => d.EditToken == token || d.ReadToken == token).FirstOrDefaultAsync(ct) is { } d
            ? Mapping.ToDomain(d) : null;

    public async Task<Preparation?> FindAsync(TeamId teamId, TournamentId tournamentId, CancellationToken ct = default) =>
        await db.Preparations.Find(d => d.TeamId == teamId.Value && d.TournamentId == tournamentId.Value)
            .FirstOrDefaultAsync(ct) is { } d ? Mapping.ToDomain(d) : null;

    public async Task SaveAsync(Preparation preparation, CancellationToken ct = default) =>
        preparation.SetPersistedVersion(await VersionedSave.SaveAsync(db.Preparations, Mapping.ToDoc(preparation), ct));
}

internal sealed class MatrixRepository(MongoContext db) : IMatrixRepository
{
    public async Task<Matrix?> FindAsync(PreparationId preparationId, ParticipantId opponentId, CancellationToken ct = default) =>
        await db.Matrices.Find(d => d.PreparationId == preparationId.Value && d.OpponentParticipantId == opponentId.Value)
            .FirstOrDefaultAsync(ct) is { } d ? Mapping.ToDomain(d) : null;

    public async Task<IReadOnlyList<Matrix>> ListAsync(PreparationId preparationId, CancellationToken ct = default) =>
        (await db.Matrices.Find(d => d.PreparationId == preparationId.Value).ToListAsync(ct))
        .Select(Mapping.ToDomain).ToList();

    public async Task SaveAsync(Matrix matrix, CancellationToken ct = default)
    {
        matrix.SetPersistedVersion(await VersionedSave.SaveAsync(db.Matrices, Mapping.ToDoc(matrix), ct));
        matrix.ClearDomainEvents();
    }
}

internal sealed class PairingSessionRepository(MongoContext db) : IPairingSessionRepository
{
    public async Task<PairingSession?> FindAsync(PreparationId preparationId, ParticipantId opponentId, CancellationToken ct = default) =>
        await db.PairingSessions.Find(d => d.PreparationId == preparationId.Value && d.OpponentParticipantId == opponentId.Value)
            .FirstOrDefaultAsync(ct) is { } d ? Mapping.ToDomain(d) : null;

    public async Task SaveAsync(PairingSession session, CancellationToken ct = default) =>
        session.SetPersistedVersion(await VersionedSave.SaveAsync(db.PairingSessions, Mapping.ToDoc(session), ct));
}
