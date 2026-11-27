namespace ProPair.Web.Services;

/// <summary>
/// Page URLs, named after what they show (German, like the UI). Relative to the base href.
/// Old links (/p/…) are forwarded by <c>LegacyRedirect</c>.
/// </summary>
public static class Routes
{
    public const string Tournaments = "";
    public static string Tournament(Guid id) => $"turniere/{id}";
    public static string Preparation(string token) => $"vorbereitung/{token}";
    public static string Matrix(string token, Guid opponentId) => $"{Preparation(token)}/gegner/{opponentId}";
    public static string Pairing(string token, Guid opponentId) => $"{Matrix(token, opponentId)}/pairing";
    public static string PairingLists(string token, Guid opponentId) => $"{Pairing(token, opponentId)}/listen";
}
