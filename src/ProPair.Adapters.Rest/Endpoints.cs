using ProPair.Application.UseCases;
using ProPair.Contracts;
using ProPair.Domain.Common;
using ProPair.Domain.Matrices;
using ProPair.Domain.Pairing;

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

        api.MapPost("/p/{token}/matrices/{opponentId:guid}/demo-fill",
            async (string token, Guid opponentId, FillDemoRatings fill, GetMatrix get, CancellationToken ct) =>
            {
                await fill.ExecuteAsync(token, new ParticipantId(opponentId), ct);
                return (await get.ExecuteAsync(token, new ParticipantId(opponentId), ct)).ToDto();
            });

        var pairing = api.MapGroup("/p/{token}/matrices/{opponentId:guid}/pairing");

        pairing.MapGet("", async (string token, Guid opponentId, GetPairing uc, CancellationToken ct) =>
            (await uc.ExecuteAsync(token, new ParticipantId(opponentId), ct)).ToDto());

        pairing.MapPost("/defenders", (string token, Guid opponentId, DefendersRequest req, PlayPairingMove uc, CancellationToken ct) =>
            Play(uc, token, opponentId, new DefendersMove(new PlayerId(req.Own), new PlayerId(req.Opponent)), ct));

        pairing.MapPost("/attackers", (string token, Guid opponentId, AttackersRequest req, PlayPairingMove uc, CancellationToken ct) =>
        {
            if (req.Own.Count != 2 || req.Opponent.Count != 2) throw new DomainException("Two attackers per team are required.");
            return Play(uc, token, opponentId, new AttackersMove(new PlayerId(req.Own[0]), new PlayerId(req.Own[1]),
                new PlayerId(req.Opponent[0]), new PlayerId(req.Opponent[1])), ct);
        });

        pairing.MapPost("/choices", (string token, Guid opponentId, ChoicesRequest req, PlayPairingMove uc, CancellationToken ct) =>
            Play(uc, token, opponentId, new ChoicesMove(new PlayerId(req.OwnAccepts), new PlayerId(req.OpponentAccepts)), ct));

        pairing.MapPost("/undo", async (string token, Guid opponentId, UndoPairingMove uc, CancellationToken ct) =>
            (await uc.ExecuteAsync(token, new ParticipantId(opponentId), ct)).ToDto());

        pairing.MapPost("/restart", async (string token, Guid opponentId, RestartPairing uc, CancellationToken ct) =>
            (await uc.ExecuteAsync(token, new ParticipantId(opponentId), ct)).ToDto());
    }

    private static async Task<PairingDto> Play(PlayPairingMove uc, string token, Guid opponentId, PairingMove move,
        CancellationToken ct) =>
        (await uc.ExecuteAsync(token, new ParticipantId(opponentId), move, ct)).ToDto();
}
