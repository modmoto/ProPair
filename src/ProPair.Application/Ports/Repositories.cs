using ProPair.Domain.Common;
using ProPair.Domain.Matrices;
using ProPair.Domain.Pairing;
using ProPair.Domain.Preparations;
using ProPair.Domain.Teams;
using ProPair.Domain.Tournaments;

namespace ProPair.Application.Ports;

public interface ITournamentRepository
{
    Task<Tournament?> GetAsync(TournamentId id, CancellationToken ct = default);
    Task<Tournament?> FindByExternalIdAsync(string externalId, CancellationToken ct = default);
    Task SaveAsync(Tournament tournament, CancellationToken ct = default);
}

public interface ITeamRepository
{
    Task<Team?> GetAsync(TeamId id, CancellationToken ct = default);
    Task<Team?> FindByNameAsync(string name, CancellationToken ct = default);
    Task SaveAsync(Team team, CancellationToken ct = default);
}

public interface IPreparationRepository
{
    Task<Preparation?> FindByTokenAsync(string token, CancellationToken ct = default);
    Task<Preparation?> FindAsync(TeamId teamId, TournamentId tournamentId, CancellationToken ct = default);
    Task SaveAsync(Preparation preparation, CancellationToken ct = default);
}

public interface IMatrixRepository
{
    Task<Matrix?> FindAsync(PreparationId preparationId, ParticipantId opponentId, CancellationToken ct = default);
    Task<IReadOnlyList<Matrix>> ListAsync(PreparationId preparationId, CancellationToken ct = default);
    Task SaveAsync(Matrix matrix, CancellationToken ct = default);
}

public interface IPairingSessionRepository
{
    Task<PairingSession?> FindAsync(PreparationId preparationId, ParticipantId opponentId, CancellationToken ct = default);
    Task SaveAsync(PairingSession session, CancellationToken ct = default);
}

/// <summary>Thrown by repositories when the aggregate was changed by someone else since it was loaded.</summary>
public sealed class ConcurrencyException() : Exception("The aggregate was modified concurrently.");
