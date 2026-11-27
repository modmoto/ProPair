using System.Text.Json;
using ProPair.Contracts;
using ProPair.Domain.Common;
using ProPair.Domain.Pairing;
using ProPair.Domain.Pairing.Advice;

namespace ProPair.Web.Services;

/// <summary>
/// Local-first pairing for one matchup. The device works without network:
/// <list type="bullet">
/// <item>moves are applied with the shared domain rules (<see cref="PairingState"/>) – no logic in the UI,</item>
/// <item>the state and an outbox of moves live in the browser; they are sent whenever the network is there,</item>
/// <item>recommendations run on the device with the shared domain advisor, fed by round values the backend
///   precomputed (the expensive part) – loaded once in the background and cached.</item>
/// </list>
/// One captain enters the pairing, so the device's move list simply replaces the stored one.
/// </summary>
public sealed class LocalPairing(ProPairApi api, Preferences store, string token, Guid opponentId)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private const int RetryMillis = 10_000;

    private PairingDto _base = default!;       // last known server data (rosters, cells, scale, …)
    private List<PairingMoveDto> _moves = [];
    private PairingState _state = default!;
    private readonly Dictionary<string, (string Key, IPairingAdviceSession Session)> _sessions = [];   // per mode
    private readonly Dictionary<string, PairingAdviceDto?> _adviceCache = [];                         // per mode + state
    private Timer? _retry;

    public PairingDto View { get; private set; } = default!;
    public SyncStatus Status { get; private set; } = SyncStatus.Saved;
    public event Action? Changed;

    private string Key(string what) => $"pairing.{token}.{opponentId}.{what}";

    // ---------- loading ----------

    /// <summary>Server first; without network the last cached copy plus the local outbox.</summary>
    public async Task<bool> LoadAsync()
    {
        var outbox = await ReadAsync<List<PairingMoveDto>>(Key("outbox"));
        try
        {
            _base = await api.GetPairingAsync(token, opponentId, null);
            await WriteAsync(Key("base"), _base);
        }
        catch (Exception e) when (e is HttpRequestException or ApiException or TaskCanceledException)
        {
            _base = await ReadAsync<PairingDto>(Key("base"))
                    ?? throw new ApiException("No connection and no data on this device yet.");
            Status = SyncStatus.Offline;
        }

        _moves = outbox ?? _base.Moves.ToList();
        Rebuild();
        if (outbox is not null) await SyncAsync(); // unsent moves from last time
        return true;
    }

    /// <summary>Read-only viewers: pick up what the captain entered (call now and then).</summary>
    public async Task RefreshAsync()
    {
        if (Status == SyncStatus.Pending) return;
        try
        {
            _base = await api.GetPairingAsync(token, opponentId, null);
            await WriteAsync(Key("base"), _base);
            _moves = _base.Moves.ToList();
            Rebuild();
            Status = SyncStatus.Saved;
            Changed?.Invoke();
        }
        catch (Exception e) when (e is HttpRequestException or ApiException or TaskCanceledException)
        {
            Status = SyncStatus.Offline;
        }
    }

    // ---------- moves (instant, offline) ----------

    /// <summary>Applies a move with the domain rules; throws <see cref="DomainException"/> if it is not allowed.</summary>
    public async Task ApplyAsync(PairingMoveDto move)
    {
        _state.Apply(ToDomain(move)); // validate first
        _moves.Add(move);
        Rebuild();
        await QueueAsync();
    }

    public async Task UndoAsync()
    {
        if (_moves.Count == 0) return;
        _moves.RemoveAt(_moves.Count - 1);
        Rebuild();
        await QueueAsync();
    }

    private async Task QueueAsync()
    {
        Status = SyncStatus.Pending;
        await WriteAsync(Key("outbox"), _moves);
        _ = SyncAsync();
    }

    /// <summary>Sends the move list if there is network; otherwise retries later. Never blocks the pairing.</summary>
    private async Task SyncAsync()
    {
        if (!View.CanEdit) return;
        var sending = _moves.ToList();
        try
        {
            var saved = await api.SyncMovesAsync(token, opponentId, sending);
            _base = saved with { Moves = sending };
            await WriteAsync(Key("base"), _base);
            if (_moves.SequenceEqual(sending))
            {
                await store.RemoveAsync(Key("outbox"));
                Status = SyncStatus.Saved;
                _retry?.Dispose();
                _retry = null;
            }
        }
        catch (Exception e) when (e is HttpRequestException or ApiException or TaskCanceledException)
        {
            Status = SyncStatus.Pending;
            _retry ??= new Timer(_ => { _retry?.Dispose(); _retry = null; _ = SyncAsync(); }, null, RetryMillis, Timeout.Infinite);
        }
        Changed?.Invoke();
    }

    // ---------- recommendations (on the device) ----------

    /// <summary>All modes the device can recommend in.</summary>
    public static readonly string[] Modes = ["optimal", "avoidminus"];

    /// <summary>Already computed recommendations for the current state, if any (instant, no calculation).</summary>
    public bool TryCached(string mode, out PairingAdviceDto? advice) => _adviceCache.TryGetValue(CacheKey(mode), out advice);

    /// <summary>
    /// Recommendations for the current state. Needs the precomputed round values once (loaded from the backend
    /// and cached); afterwards everything runs offline. Results are remembered per mode and state,
    /// so switching modes or going back to a state is instant.
    /// </summary>
    public async Task<PairingAdviceDto?> AdviseAsync(string mode)
    {
        if (_state.Step == PairingStep.Done) return null;
        var key = CacheKey(mode);
        if (_adviceCache.TryGetValue(key, out var cached)) return cached;

        var session = await AdviceSessionAsync(mode);
        if (session is null) return null;
        await Task.Yield(); // let the UI render the tap before the calculation
        if (_adviceCache.TryGetValue(key, out cached)) return cached; // computed meanwhile
        var advice = AdviceMapping.ToDto(session.Advise(_state), mode, View.Scale);
        _adviceCache[key] = advice;
        return advice;
    }

    /// <summary>Loads the round values of every mode (one network call each, then cached on the device).</summary>
    public async Task PreloadAsync()
    {
        foreach (var mode in Modes) await AdviceSessionAsync(mode);
    }

    private string CacheKey(string mode) =>
        $"{mode}|{CellsHash()}|{string.Join(";", _moves.Select(m => $"{m.Step}:{string.Join(",", m.Own)}:{string.Join(",", m.Opponent)}"))}";

    private async Task<IPairingAdviceSession?> AdviceSessionAsync(string mode)
    {
        var key = $"values.{mode}.{CellsHash()}";
        if (_sessions.TryGetValue(mode, out var known) && known.Key == key) return known.Session;

        var values = await ReadAsync<RoundValuesDto>(Key(key));
        if (values is null)
        {
            try
            {
                values = await api.GetRoundValuesAsync(token, opponentId, mode);
                await WriteAsync(Key(key), values);
            }
            catch (Exception e) when (e is HttpRequestException or ApiException or TaskCanceledException)
            {
                return null; // offline and never loaded: pairing works, recommendations come with the network
            }
        }

        var own = View.Own.Players.Select(p => new PlayerId(p.Id)).ToList();
        var opp = View.Opponent.Players.Select(p => new PlayerId(p.Id)).ToList();
        if (!values.OwnOrder.SequenceEqual(own.Select(p => p.Value)) || !values.OpponentOrder.SequenceEqual(opp.Select(p => p.Value)))
            return null; // rosters changed – values don't fit

        var table = new RoundValues(values.Keys.Zip(values.Values).ToDictionary(kv => kv.First, kv => kv.Second));
        var session = AdviceModes.Create(Enum.Parse<AdviceMode>(mode, ignoreCase: true)).Start(own, opp, Score, table);
        _sessions[mode] = (key, session);
        return session;
    }

    private double Score(PlayerId own, PlayerId opp) => AdviceMapping.Score(View, own.Value, opp.Value);

    /// <summary>Stable hash of the ratings, so cached values are dropped when the matrix changed.</summary>
    private string CellsHash()
    {
        var hash = 2166136261u;
        foreach (var c in View.Cells.OrderBy(c => c.OwnPlayerId).ThenBy(c => c.OpponentPlayerId))
        foreach (var ch in $"{c.OwnPlayerId}{c.OpponentPlayerId}{c.LevelKey};")
            hash = (hash ^ ch) * 16777619u;
        return hash.ToString("x8");
    }

    // ---------- view ----------

    /// <summary>Replays the moves with the domain rules and projects the state onto the DTO the page renders.</summary>
    private void Rebuild()
    {
        var own = _base.Own.Players.Select(p => new PlayerId(p.Id)).ToList();
        var opp = _base.Opponent.Players.Select(p => new PlayerId(p.Id)).ToList();
        _state = _moves.Aggregate(PairingState.Start(own, opp), (s, m) => s.Apply(ToDomain(m)));

        var games = _state.Games.Select(g => new GameDto(g.Own.Value, g.Opponent.Value,
            AdviceMapping.Score(_base, g.Own.Value, g.Opponent.Value))).ToList();
        View = _base with
        {
            Step = _state.Step.ToString().ToLowerInvariant(),
            Round = _state.Round,
            RemainingOwn = _state.RemainingOwn.Select(p => p.Value).ToList(),
            RemainingOpponent = _state.RemainingOpponent.Select(p => p.Value).ToList(),
            OwnDefender = _state.OwnDefender?.Value,
            OpponentDefender = _state.OpponentDefender?.Value,
            OwnAttackers = _state.OwnAttackers.Select(p => p.Value).ToList(),
            OpponentAttackers = _state.OpponentAttackers.Select(p => p.Value).ToList(),
            Games = games,
            FixedScore = games.Sum(g => g.Score),
            Advice = null,
            MoveCount = _moves.Count,
            Moves = _moves.ToList(),
        };
    }

    private static PairingMove ToDomain(PairingMoveDto m) => m.Step switch
    {
        "defenders" => new DefendersMove(new PlayerId(m.Own[0]), new PlayerId(m.Opponent[0])),
        "attackers" => new AttackersMove(new PlayerId(m.Own[0]), new PlayerId(m.Own[1]),
            new PlayerId(m.Opponent[0]), new PlayerId(m.Opponent[1])),
        _ => new ChoicesMove(new PlayerId(m.Own[0]), new PlayerId(m.Opponent[0])),
    };

    // ---------- browser storage ----------

    private async Task<T?> ReadAsync<T>(string key) where T : class
    {
        var json = await store.GetStringAsync(key);
        if (json is null) return null;
        try { return JsonSerializer.Deserialize<T>(json, Json); }
        catch (JsonException) { return null; }
    }

    private Task WriteAsync<T>(string key, T value) => store.SetStringAsync(key, JsonSerializer.Serialize(value, Json));
}

public enum SyncStatus { Saved, Pending, Offline }
