using Microsoft.JSInterop;

namespace ProPair.Web.Services;

/// <summary>Remembers the name of the person editing (no login), stored in the browser.</summary>
public sealed class AuthorStore(IJSRuntime js)
{
    private const string Key = "propair.author";
    private string? _name;

    public async Task<string?> GetAsync()
    {
        if (_name is not null) return _name;
        try { return _name = await js.InvokeAsync<string?>("localStorage.getItem", Key); }
        catch (JSException) { return null; }
    }

    public async Task SetAsync(string name)
    {
        _name = name.Trim();
        try { await js.InvokeVoidAsync("localStorage.setItem", Key, _name); }
        catch (JSException) { /* storage unavailable: keep name for this session only */ }
    }
}
