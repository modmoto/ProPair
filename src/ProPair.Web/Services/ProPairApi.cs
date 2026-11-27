using System.Net.Http.Json;
using ProPair.Contracts;

namespace ProPair.Web.Services;

/// <summary>Typed client for the ProPair REST API. The UI talks to the backend only through this.</summary>
public sealed class ProPairApi(HttpClient http)
{
    public async Task<IReadOnlyList<SourceTournamentDto>> GetSourceTournamentsAsync() =>
        await http.GetFromJsonAsync<List<SourceTournamentDto>>("api/source-tournaments") ?? [];

    public async Task<TournamentDto> ImportAsync(string externalId) =>
        await ReadAsync<TournamentDto>(await http.PostAsJsonAsync("api/tournaments/import", new ImportTournamentRequest(externalId)));

    public async Task<PreparationCreatedDto> StartPreparationAsync(Guid tournamentId, Guid participantId) =>
        await ReadAsync<PreparationCreatedDto>(await http.PostAsJsonAsync(
            $"api/tournaments/{tournamentId}/preparations", new StartPreparationRequest(participantId)));

    public async Task<PreparationDto> GetPreparationAsync(string token) =>
        await ReadAsync<PreparationDto>(await http.GetAsync($"api/p/{token}"));

    public async Task<MatrixDto> GetMatrixAsync(string token, Guid opponentId) =>
        await ReadAsync<MatrixDto>(await http.GetAsync($"api/p/{token}/matrices/{opponentId}"));

    public async Task SetRatingAsync(string token, Guid opponentId, SetRatingRequest request) =>
        await EnsureAsync(await http.PutAsJsonAsync($"api/p/{token}/matrices/{opponentId}/cells", request));

    private static string PairingUrl(string token, Guid opponentId) => $"api/p/{token}/matrices/{opponentId}/pairing";

    public async Task<PairingDto> GetPairingAsync(string token, Guid opponentId) =>
        await ReadAsync<PairingDto>(await http.GetAsync(PairingUrl(token, opponentId)));

    public async Task<PairingDto> SetDefendersAsync(string token, Guid opponentId, DefendersRequest request) =>
        await ReadAsync<PairingDto>(await http.PostAsJsonAsync($"{PairingUrl(token, opponentId)}/defenders", request));

    public async Task<PairingDto> SetAttackersAsync(string token, Guid opponentId, AttackersRequest request) =>
        await ReadAsync<PairingDto>(await http.PostAsJsonAsync($"{PairingUrl(token, opponentId)}/attackers", request));

    public async Task<PairingDto> SetChoicesAsync(string token, Guid opponentId, ChoicesRequest request) =>
        await ReadAsync<PairingDto>(await http.PostAsJsonAsync($"{PairingUrl(token, opponentId)}/choices", request));

    public async Task<PairingDto> UndoPairingAsync(string token, Guid opponentId) =>
        await ReadAsync<PairingDto>(await http.PostAsync($"{PairingUrl(token, opponentId)}/undo", null));

    public async Task<PairingDto> RestartPairingAsync(string token, Guid opponentId) =>
        await ReadAsync<PairingDto>(await http.PostAsync($"{PairingUrl(token, opponentId)}/restart", null));

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        await EnsureAsync(response);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private static async Task EnsureAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>().ConfigureAwait(false);
        throw new ApiException(error?.Message ?? response.ReasonPhrase ?? "Request failed");
    }
}

public sealed class ApiException(string message) : Exception(message);
