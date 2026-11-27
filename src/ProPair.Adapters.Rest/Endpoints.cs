using ProPair.Application.UseCases;
using ProPair.Contracts;
using ProPair.Domain.Common;
using ProPair.Domain.Matrices;
using ProPair.Domain.Pairing;
using ProPair.Domain.Pairing.Advice;
using ProPair.Domain.Preparations;

namespace ProPair.Adapters.Rest;

internal static class Endpoints
{
    public static void MapProPairApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/source-tournaments", async (SearchSourceTournaments uc, CancellationToken ct) =>
            (await uc.ExecuteAsync(ct))
            .Select(t => new SourceTournamentDto(t.ExternalId, t.Name, t.StartDate, t.TeamSize, t.TeamCount)));

        api.MapPost("/tournaments/import", async (ImportTournamentRequest req, ImportTournament uc, CancellationToken ct) =>
            (await uc.ExecuteAsync(req.ExternalId, ct)).ToDto());

        api.MapPost("/tournaments/{id:guid}/preparations",
            async (Guid id, StartPreparationRequest req, StartPreparation uc, CancellationToken ct) =>
            {
                var p = await uc.ExecuteAsync(new TournamentId(id), new ParticipantId(req.ParticipantId), ct);
                return new PreparationCreatedDto(p.EditToken.Value, p.ReadToken.Value);
            });

        api.MapGet("/p/{token}", async (string token, GetPreparation uc, CancellationToken ct) =>
            (await uc.ExecuteAsync(token, ct)).ToDto());

        api.MapGet("/p/{token}/matrices/{opponentId:guid}",
            async (string token, Guid opponentId, GetMatrix uc, CancellationToken ct) =>
                (await uc.ExecuteAsync(token, new ParticipantId(opponentId), ct)).ToDto());

        api.MapPut("/p/{token}/matrices/{opponentId:guid}/cells",
            async (string token, Guid opponentId, SetRatingRequest req, SetRating uc, CancellationToken ct) =>
            {
                var cell = new CellKey(new PlayerId(req.OwnPlayerId), new PlayerId(req.OpponentPlayerId));
                await uc.ExecuteAsync(token, new ParticipantId(opponentId), cell, req.LevelKey, req.Author, ct);
                return Results.NoContent();
            });

        api.MapPut("/p/{token}/matrices/{opponentId:guid}/cells/{own:guid}/{opp:guid}/note",
            async (string token, Guid opponentId, Guid own, Guid opp, SetNoteRequest req, SetCellNote uc, CancellationToken ct) =>
            {
                await uc.ExecuteAsync(token, new ParticipantId(opponentId), new CellKey(new PlayerId(own), new PlayerId(opp)),
                    req.Text, req.Author, ct);
                return Results.NoContent();
            });

        api.MapGet("/p/{token}/matrices/{opponentId:guid}/cells/{own:guid}/{opp:guid}/history",
            async (string token, Guid opponentId, Guid own, Guid opp, GetCellHistory uc, CancellationToken ct) =>
                (await uc.ExecuteAsync(token, new ParticipantId(opponentId), new CellKey(new PlayerId(own), new PlayerId(opp)), ct))
                .Select(h => new RatingChangeDto(h.OldLevelKey, h.NewLevelKey, h.Author, h.OccurredAt)));

        api.MapPut("/p/{token}/players/{playerId:guid}/note",
            async (string token, Guid playerId, SetNoteRequest req, SetPlayerNote uc, CancellationToken ct) =>
            {
                await uc.ExecuteAsync(token, new PlayerId(playerId), req.Text, req.Author, ct);
                return Results.NoContent();
            });

        api.MapPut("/p/{token}/players/{playerId:guid}/marks/{lineIndex:int}",
            async (string token, Guid playerId, int lineIndex, SetListMarkRequest req, SetListMark uc, CancellationToken ct) =>
            {
                MarkColor? color = req.Color is null ? null
                    : Enum.TryParse<MarkColor>(req.Color, ignoreCase: true, out var c) ? c
                    : throw new DomainException($"Unknown color '{req.Color}'.");
                await uc.ExecuteAsync(token, new PlayerId(playerId), lineIndex, req.LineText, color, req.Note, req.Author, ct);
                return Results.NoContent();
            });

        api.MapPost("/p/{token}/matrices/{opponentId:guid}/demo-fill",
            async (string token, Guid opponentId, FillDemoRatings fill, GetMatrix get, CancellationToken ct) =>
            {
                await fill.ExecuteAsync(token, new ParticipantId(opponentId), ct);
                return (await get.ExecuteAsync(token, new ParticipantId(opponentId), ct)).ToDto();
            });

        var pairing = api.MapGroup("/p/{token}/matrices/{opponentId:guid}/pairing");

        // ?advice=optimal|avoidminus|safe adds recommendations (only when the UI shows them)
        pairing.MapGet("", async (string token, Guid opponentId, string? advice, GetPairing uc, CancellationToken ct) =>
            (await uc.ExecuteAsync(token, new ParticipantId(opponentId), Mode(advice), ct)).ToDto());

        pairing.MapPost("/defenders", (string token, Guid opponentId, string? advice, DefendersRequest req, PlayPairingMove uc, CancellationToken ct) =>
            Play(uc, token, opponentId, new DefendersMove(new PlayerId(req.Own), new PlayerId(req.Opponent)), Mode(advice), ct));

        pairing.MapPost("/attackers", (string token, Guid opponentId, string? advice, AttackersRequest req, PlayPairingMove uc, CancellationToken ct) =>
        {
            if (req.Own.Count != 2 || req.Opponent.Count != 2) throw new DomainException("Two attackers per team are required.");
            return Play(uc, token, opponentId, new AttackersMove(new PlayerId(req.Own[0]), new PlayerId(req.Own[1]),
                new PlayerId(req.Opponent[0]), new PlayerId(req.Opponent[1])), Mode(advice), ct);
        });

        pairing.MapPost("/choices", (string token, Guid opponentId, string? advice, ChoicesRequest req, PlayPairingMove uc, CancellationToken ct) =>
            Play(uc, token, opponentId, new ChoicesMove(new PlayerId(req.OwnAccepts), new PlayerId(req.OpponentAccepts)), Mode(advice), ct));

        pairing.MapPost("/undo", async (string token, Guid opponentId, string? advice, UndoPairingMove uc, CancellationToken ct) =>
            (await uc.ExecuteAsync(token, new ParticipantId(opponentId), Mode(advice), ct)).ToDto());

        // local-first clients: send the whole move list whenever the network is there
        pairing.MapPut("/moves", async (string token, Guid opponentId, SyncMovesRequest req, SyncPairingMoves uc, CancellationToken ct) =>
            (await uc.ExecuteAsync(token, new ParticipantId(opponentId), req.Moves.Select(m => m.ToDomain()).ToList(), ct)).ToDto());

        // precomputed round values so the device can recommend offline (?mode=optimal|avoidminus|safe)
        pairing.MapGet("/values", async (string token, Guid opponentId, string? mode, GetRoundValues uc, CancellationToken ct) =>
        {
            var adviceMode = Mode(mode ?? "optimal") ?? AdviceMode.Optimal;
            var (values, own, opp) = await uc.ExecuteAsync(token, new ParticipantId(opponentId), adviceMode, ct);
            var ordered = values.Values.OrderBy(kv => kv.Key).ToList();
            return new RoundValuesDto(adviceMode.ToString().ToLowerInvariant(), own.Select(p => p.Value).ToList(),
                opp.Select(p => p.Value).ToList(), ordered.Select(kv => kv.Key).ToList(), ordered.Select(kv => kv.Value).ToList());
        });

        pairing.MapPost("/restart", async (string token, Guid opponentId, RestartPairing uc, CancellationToken ct) =>
            (await uc.ExecuteAsync(token, new ParticipantId(opponentId), ct)).ToDto());
    }

    private static async Task<PairingDto> Play(PlayPairingMove uc, string token, Guid opponentId, PairingMove move,
        AdviceMode? mode, CancellationToken ct) =>
        (await uc.ExecuteAsync(token, new ParticipantId(opponentId), move, mode, ct)).ToDto();

    /// <summary>?advice=optimal|avoidminus|safe (true = optimal); missing = no recommendations.</summary>
    private static AdviceMode? Mode(string? advice) => advice?.ToLowerInvariant() switch
    {
        null or "" or "false" => null,
        "true" or "optimal" => AdviceMode.Optimal,
        "avoidminus" => AdviceMode.AvoidMinus,
        "safe" => AdviceMode.Safe,
        _ => throw new DomainException($"Unknown advice mode '{advice}'."),
    };
}
