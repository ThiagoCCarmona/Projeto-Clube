using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Clube.Shared.DTOs;

namespace Clube.Mobile.Services;

public interface IApiClient
{
    void SetAuthToken(string? token);
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<AssociadoDto?> AutoCadastrarSocioAsync(RegistroPublicoRequest request);
    Task<SyncPushResponse?> PushOutboxAsync(SyncPushRequest request);
    Task<SyncPullResponse?> PullUpdatesAsync(Guid? clubeId, DateTime? since = null);
}

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ApiClient()
    {
        var baseUrl = DeviceInfo.Platform == DevicePlatform.Android 
            ? "http://10.0.2.2:5000" 
            : "http://localhost:5000";

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    public void SetAuthToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
        else
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);
            if (response.IsSuccessStatusCode)
            {
                var auth = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
                if (auth != null && !string.IsNullOrEmpty(auth.Token))
                    SetAuthToken(auth.Token);
                return auth;
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<AssociadoDto?> AutoCadastrarSocioAsync(RegistroPublicoRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/registrar", request);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<AssociadoDto>(JsonOptions);

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<SyncPushResponse?> PushOutboxAsync(SyncPushRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/sync/push", request);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<SyncPushResponse>(JsonOptions);

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<SyncPullResponse?> PullUpdatesAsync(Guid? clubeId, DateTime? since = null)
    {
        try
        {
            var url = "/api/sync/pull?";
            if (clubeId.HasValue) url += $"clubeId={clubeId.Value}&";
            if (since.HasValue) url += $"since={Uri.EscapeDataString(since.Value.ToString("o"))}&";

            var response = await _httpClient.GetAsync(url.TrimEnd('&', '?'));
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<SyncPullResponse>(JsonOptions);

            return null;
        }
        catch
        {
            return null;
        }
    }
}
