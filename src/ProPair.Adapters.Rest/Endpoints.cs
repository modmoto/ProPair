using ProPair.Application.UseCases;
using ProPair.Contracts;
using ProPair.Domain.Common;
using ProPair.Domain.Matrices;

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
    }
}
