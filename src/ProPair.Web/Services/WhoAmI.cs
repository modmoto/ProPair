using Microsoft.JSInterop;

namespace ProPair.Web.Services;

/// <summary>
/// Who is using this device, per own team (no login yet): the chosen player of the team, stored in the browser.
/// Their name is the author of ratings, notes and marks.
/// </summary>
public sealed class WhoAmI(IJSRuntime js)
{
    private static string Key(Guid teamId) => $"propair.me.{teamId}";

    public async Task<Guid?> GetAsync(Guid teamId)
    {
        try { return Guid.TryParse(await js.InvokeAsync<string?>("localStorage.getItem", Key(teamId)), out var id) ? id : null; }
        catch (JSException) { return null; }
    }

    public async Task SetAsync(Guid teamId, Guid playerId)
    {
        try { await js.InvokeVoidAsync("localStorage.setItem", Key(teamId), playerId.ToString()); }
        catch (JSException) { /* storage unavailable: the choice lasts for this page only */ }
    }
}
