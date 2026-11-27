namespace ProPair.Web.Services;

/// <summary>
/// Page URLs, named after what they show. Relative to the base href.
/// </summary>
public static class Routes
{
    public const string Tournaments = "";
    public static string Tournament(Guid id) => $"tournaments/{id}";
    public static string Preparation(string token) => $"preparation/{token}";
    public static string Matrix(string token, Guid opponentId) => $"{Preparation(token)}/opponent/{opponentId}";
    public static string Pairing(string token, Guid opponentId) => $"{Matrix(token, opponentId)}/pairing";
    public static string PairingLists(string token, Guid opponentId) => $"{Pairing(token, opponentId)}/lists";
}
