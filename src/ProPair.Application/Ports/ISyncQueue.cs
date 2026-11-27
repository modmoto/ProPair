namespace ProPair.Application.Ports;

/// <summary>Hands work to the separate sync process (Hangfire). The API only enqueues, it never syncs itself.</summary>
public interface ISyncQueue
{
    void RequestTournamentSync(string externalId);
}
