using ProPair.Application.Ports;
using ProPair.Domain.Common;
using ProPair.Domain.Matrices;
using ProPair.Domain.Pairing;
using ProPair.Domain.Preparations;
using ProPair.Domain.Teams;
using ProPair.Domain.Tournaments;

namespace ProPair.Application.Tests;

internal sealed class InMemoryStore : ITournamentRepository, ITeamRepository, IPreparationRepository, IMatrixRepository, IPairingSessionRepository
{
    private readonly List<PairingSession> _sessions = [];
    private readonly List<Tournament> _tournaments = [];
    private readonly List<Team> _teams = [];
    private readonly List<Preparation> _preparations = [];
    private readonly List<Matrix> _matrices = [];

    public Task<Tournament?> GetAsync(TournamentId id, CancellationToken ct = default) =>
        Task.FromResult(_tournaments.FirstOrDefault(t => t.Id == id));
    public Task<Tournament?> FindByExternalIdAsync(string externalId, CancellationToken ct = default) =>
        Task.FromResult(_tournaments.FirstOrDefault(t => t.ExternalId == externalId));
    public Task<IReadOnlyList<Tournament>> ListAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Tournament>>(_tournaments.ToList());

    public Task SaveAsync(Tournament tournament, CancellationToken ct = default) => Upsert(_tournaments, tournament);

    public Task<Team?> GetAsync(TeamId id, CancellationToken ct = default) =>
        Task.FromResult(_teams.FirstOrDefault(t => t.Id == id));
    public Task<Team?> FindByNameAsync(string name, CancellationToken ct = default) =>
        Task.FromResult(_teams.FirstOrDefault(t => t.Name == name));
    public Task SaveAsync(Team team, CancellationToken ct = default) => Upsert(_teams, team);

    public Task<Preparation?> FindByTokenAsync(string token, CancellationToken ct = default) =>
        Task.FromResult(_preparations.FirstOrDefault(p => p.AccessFor(token) != AccessLevel.None));
    public Task<Preparation?> FindAsync(TeamId teamId, TournamentId tournamentId, CancellationToken ct = default) =>
        Task.FromResult(_preparations.FirstOrDefault(p => p.TeamId == teamId && p.TournamentId == tournamentId));
    public Task SaveAsync(Preparation preparation, CancellationToken ct = default) => Upsert(_preparations, preparation);

    public Task<Matrix?> FindAsync(PreparationId preparationId, ParticipantId opponentId, CancellationToken ct = default) =>
        Task.FromResult(_matrices.FirstOrDefault(m => m.PreparationId == preparationId && m.OpponentParticipantId == opponentId));
    public Task<IReadOnlyList<Matrix>> ListAsync(PreparationId preparationId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Matrix>>(_matrices.Where(m => m.PreparationId == preparationId).ToList());
    public Task SaveAsync(Matrix matrix, CancellationToken ct = default) => Upsert(_matrices, matrix);

    Task<PairingSession?> IPairingSessionRepository.FindAsync(PreparationId preparationId, ParticipantId opponentId, CancellationToken ct) =>
        Task.FromResult(_sessions.FirstOrDefault(m => m.PreparationId == preparationId && m.OpponentParticipantId == opponentId));
    public Task SaveAsync(PairingSession session, CancellationToken ct = default) => Upsert(_sessions, session);

    private static Task Upsert<T>(List<T> list, T item) where T : class
    {
        if (!list.Contains(item)) list.Add(item);
        return Task.CompletedTask;
    }
}

internal sealed class FixedClock : IClock
{
    public DateTimeOffset Now => new(2026, 11, 27, 12, 0, 0, TimeSpan.Zero);
}
