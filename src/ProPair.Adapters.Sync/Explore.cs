using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ProPair.Adapters.Sync;

/// <summary>
/// "Hello World" against the New Recruit API: fetches systems, upcoming T9A tournaments and one tournament,
/// writes the raw JSON to a folder and prints the shape. Never prints the credentials.
/// Usage: dotnet run --project src/ProPair.Adapters.Sync -- explore &lt;output folder&gt;
/// </summary>
internal static class Explore
{
    private const string BaseUrl = "https://www.newrecruit.eu/api/";

    /// <summary>One raw call, e.g. explore-call reports '{"id":"…"}' – for checking what an endpoint returns.</summary>
    public static async Task CallOnceAsync(IConfiguration config, string outDir, string path, string body)
    {
        Directory.CreateDirectory(outDir);
        using var http = Client(config);
        var response = await http.PostAsync(path, new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
        var text = await response.Content.ReadAsStringAsync();
        var file = Path.Combine(outDir, $"{path}-raw.json");
        await File.WriteAllTextAsync(file, text);
        Console.WriteLine($"POST {path} → {(int)response.StatusCode}, {text.Length} bytes → {file}");
    }

    private static HttpClient Client(IConfiguration config)
    {
        var http = new HttpClient { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(60) };
        http.DefaultRequestHeaders.Add("NR-Login", config["NewRecruit:Login"] ?? throw new InvalidOperationException("NewRecruit:Login missing"));
        http.DefaultRequestHeaders.Add("NR-Password", config["NewRecruit:Password"] ?? throw new InvalidOperationException("NewRecruit:Password missing"));
        return http;
    }

    public static async Task RunAsync(IConfiguration config, string outDir)
    {
        Directory.CreateDirectory(outDir);
        using var http = new HttpClient { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(60) };
        http.DefaultRequestHeaders.Add("NR-Login", config["NewRecruit:Login"] ?? throw new InvalidOperationException("NewRecruit:Login missing"));
        http.DefaultRequestHeaders.Add("NR-Password", config["NewRecruit:Password"] ?? throw new InvalidOperationException("NewRecruit:Password missing"));

        var systems = await CallAsync(http, "systems", new { }, outDir, "systems.json");
        // New Recruit has several T9A systems (one per edition) – ask each for the next 12 months
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var t9aSystems = (systems as JsonArray ?? [])
            .Where(x => IsT9A(x?["name"]?.ToString()))
            .Select(x => (Id: x!["id"]!.GetValue<long>(), Name: x["name"]!.ToString()))
            .ToList();
        var tournaments = new JsonArray();
        foreach (var (systemId, name) in t9aSystems)
        {
            var list = await CallAsync(http, "tournaments",
                new { start = today.ToString("yyyy-MM-dd"), end = today.AddMonths(12).ToString("yyyy-MM-dd"), id_game_system = systemId },
                outDir, $"tournaments-{systemId}.json");
            var found = list?["tournaments"]?.AsArray() ?? [];
            Console.WriteLine($"system {systemId} \"{name}\": total={list?["total"]}, page 1: {found.Count}");
            foreach (var t in found.Take(8))
                Console.WriteLine($"  {t?["_id"]}  {t?["start"]}  status={t?["status"]}  {t?["name"]}");
            foreach (var t in found) tournaments.Add(t?.DeepClone());
        }

        // details one by one (with a pause – New Recruit logs every request); summary per tournament
        var seen = new HashSet<string>();
        foreach (var t in tournaments)
        {
            var id = t?["_id"]?.ToString();
            if (id is null || !seen.Add(id)) continue;
            var detail = await CallAsync(http, "tournament", new { id }, outDir, $"tournament-{id}.json", quiet: true);
            var teams = detail?["teams"]?.AsArray() ?? [];
            var players = teams.Select(x => x?["players"]?.AsArray().Count ?? 0).ToList();
            var lists = teams.SelectMany(x => x?["players"]?.AsArray() ?? []).Select(p => p?["lists"]?.AsArray().Count ?? 0).ToList();
            Console.WriteLine($"{id} type={detail?["type"]} showlists={detail?["showlists"]} teams={teams.Count} " +
                              $"players/team={string.Join(",", players.Distinct().Order())} lists/player={string.Join(",", lists.Distinct().Order())} " +
                              $"start={t?["start"]?.ToString()[..10]} status={t?["status"]} {t?["name"]}");
            await Task.Delay(400);
        }
    }

    private static async Task<JsonNode?> CallAsync(HttpClient http, string path, object body, string outDir, string file, bool quiet = false)
    {
        var response = await http.PostAsJsonAsync(path, body);
        var text = await response.Content.ReadAsStringAsync();
        await File.WriteAllTextAsync(Path.Combine(outDir, file), text);
        if (!quiet || !response.IsSuccessStatusCode)
            Console.WriteLine($"POST {path} → {(int)response.StatusCode}, {text.Length} bytes → {file}");
        try { return JsonNode.Parse(text); }
        catch (JsonException) { return null; }
    }

    private static bool IsT9A(string? name) =>
        name is not null && !name.Contains("Warhammer", StringComparison.OrdinalIgnoreCase) &&
        (name.Contains("Ninth Age", StringComparison.OrdinalIgnoreCase) || name.Contains("9th Age", StringComparison.OrdinalIgnoreCase))
        && !name.Contains("Essence of War", StringComparison.OrdinalIgnoreCase);
}
