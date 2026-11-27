using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ProPair.Application.Ports;

namespace ProPair.Adapters.NewRecruit.Api;

/// <summary>
/// New Recruit API (POST + JSON, login via headers). Requests go one at a time with a pause in between.
/// Only translates – which tournaments we keep is decided in the use cases.
/// </summary>
public sealed class NewRecruitClient(HttpClient http, NewRecruitOptions options) : ITournamentSource
{
    private const int PageSize = 50;
    private static readonly SemaphoreSlim OneAtATime = new(1, 1);

    public async Task<IReadOnlyList<SourceTournamentSummary>> SearchAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var result = new Dictionary<string, SourceTournamentSummary>();
        for (var page = 1; ; page++)
        {
            var response = await PostAsync<TournamentsResponse>("tournaments", new
            {
                start = from.ToString("yyyy-MM-dd"), end = to.ToString("yyyy-MM-dd"),
                id_game_system = options.GameSystemId, page,
            }, ct);
            var items = response?.Tournaments ?? [];
            var added = 0;
            foreach (var t in items)
                if (result.TryAdd(t.Id, new SourceTournamentSummary(t.Id, t.Name.Trim(), ToDate(t.Start), ToDate(t.End),
                        (SourceStatus)t.Status))) added++;

            // stop on a short page, a page with nothing new (paging ignored) or when we have everything
            if (items.Count < PageSize || added == 0 || result.Count >= (response?.Total ?? 0)) break;
        }
        return result.Values.ToList();
    }

    public async Task<SourceTournament?> GetAsync(string externalId, CancellationToken ct = default)
    {
        var t = await PostAsync<TournamentResponse>("tournament", new { id = externalId }, ct);
        if (t?.Id is null) return null;
        return new SourceTournament(t.Id, t.Name.Trim(), null, null, IsTeamTournament: t.Type == 1, ListsVisible: t.ShowLists,
            (t.Teams ?? []).Select(team => new SourceTeam(team.Name.Trim(),
                (team.Players ?? []).Select(p => new SourcePlayer(p.Name.Trim(),
                    (p.Lists ?? []).Where(l => !string.IsNullOrWhiteSpace(l)).ToList())).ToList())).ToList());
    }

    private async Task<T?> PostAsync<T>(string path, object body, CancellationToken ct)
    {
        await OneAtATime.WaitAsync(ct);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(options.BaseUrl), path))
                { Content = JsonContent.Create(body) };
            request.Headers.Add("NR-Login", options.Login);
            request.Headers.Add("NR-Password", options.Password);
            using var response = await http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(ct);
        }
        finally
        {
            await Task.Delay(options.DelayMs, CancellationToken.None);
            OneAtATime.Release();
        }
    }

    private static DateOnly? ToDate(DateTimeOffset? value) => value is { } v ? DateOnly.FromDateTime(v.UtcDateTime) : null;

    private sealed record TournamentsResponse(
        [property: JsonPropertyName("tournaments")] List<TournamentSummary>? Tournaments,
        [property: JsonPropertyName("total")] int Total);

    private sealed record TournamentSummary(
        [property: JsonPropertyName("_id")] string Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("start")] DateTimeOffset? Start,
        [property: JsonPropertyName("end")] DateTimeOffset? End,
        [property: JsonPropertyName("status")] int Status);

    private sealed record TournamentResponse(
        [property: JsonPropertyName("_id")] string? Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("showlists")] bool ShowLists,
        [property: JsonPropertyName("type")] int Type,
        [property: JsonPropertyName("teams")] List<Team>? Teams);

    private sealed record Team(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("players")] List<TeamPlayer>? Players);

    private sealed record TeamPlayer(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("lists")] List<string>? Lists);
}
