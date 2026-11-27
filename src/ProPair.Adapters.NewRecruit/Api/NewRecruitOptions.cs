namespace ProPair.Adapters.NewRecruit.Api;

/// <summary>Login comes from user secrets / env vars (NewRecruit__Login, NewRecruit__Password) – never from appsettings.</summary>
public sealed class NewRecruitOptions
{
    public string BaseUrl { get; set; } = "https://www.newrecruit.eu/api/";
    public string Login { get; set; } = "";
    public string Password { get; set; } = "";

    /// <summary>"The 9th Age 3rd Edition". Older T9A systems are ignored.</summary>
    public long GameSystemId { get; set; } = 736602462;

    /// <summary>Pause between two requests – New Recruit logs every call, so we stay polite.</summary>
    public int DelayMs { get; set; } = 400;
}
