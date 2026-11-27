using Microsoft.JSInterop;

namespace ProPair.Web.Services;

/// <summary>Per-device switches, stored in the browser (e.g. whether recommendations are shown).</summary>
public sealed class Preferences(IJSRuntime js)
{
    public async Task<bool> GetAsync(string key, bool fallback = false)
    {
        try { return await js.InvokeAsync<string?>("localStorage.getItem", $"propair.{key}") is { } v ? v == "1" : fallback; }
        catch (JSException) { return fallback; }
    }

    public async Task<string?> GetStringAsync(string key)
    {
        try { return await js.InvokeAsync<string?>("localStorage.getItem", $"propair.{key}"); }
        catch (JSException) { return null; }
    }

    public async Task SetStringAsync(string key, string value)
    {
        try { await js.InvokeVoidAsync("localStorage.setItem", $"propair.{key}", value); }
        catch (JSException) { /* storage unavailable */ }
    }

    public async Task SetAsync(string key, bool value)
    {
        try { await js.InvokeVoidAsync("localStorage.setItem", $"propair.{key}", value ? "1" : "0"); }
        catch (JSException) { /* storage unavailable: setting lasts for this page only */ }
    }
}
