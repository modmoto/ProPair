using ProPair.Application;
using ProPair.Application.Ports;
using ProPair.Contracts;
using ProPair.Domain.Common;

namespace ProPair.Adapters.Rest;

internal static class ErrorHandling
{
    public static IApplicationBuilder UseDomainErrors(this IApplicationBuilder app) =>
        app.Use(async (ctx, next) =>
        {
            try { await next(ctx); }
            catch (Exception e) when (StatusFor(e) is { } status && !ctx.Response.HasStarted)
            {
                ctx.Response.StatusCode = status;
                var message = e is ConcurrencyException ? "Changed by someone else at the same time – please try again." : e.Message;
                await ctx.Response.WriteAsJsonAsync(new ErrorDto(message));
            }
        });

    private static int? StatusFor(Exception e) => e switch
    {
        DomainException => StatusCodes.Status400BadRequest,
        NotFoundException => StatusCodes.Status404NotFound,
        ForbiddenException => StatusCodes.Status403Forbidden,
        ConcurrencyException => StatusCodes.Status409Conflict,
        _ => null,
    };
}
