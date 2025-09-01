using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IISApp.Services
{
    public class ApiService
    {
        private readonly HttpClient _http;

        public string? AccessToken { get; private set; }
        public string? RefreshToken { get; private set; }

        public ApiService(string baseUrl)
        {
            _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            var payload = new { username, password };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _http.PostAsync("/api/auth/login", content);
            if (!response.IsSuccessStatusCode)
                return false;

            var body = await response.Content.ReadAsStringAsync();
            try
            {
                using var doc = JsonDocument.Parse(body);
                AccessToken = doc.RootElement.GetProperty("accessToken").GetString();
                RefreshToken = doc.RootElement.TryGetProperty("refreshToken", out var refresh) ? refresh.GetString() : null;
            }
            catch (JsonException)
            {
                // Some services return the JWT as plain text rather than JSON
                AccessToken = body.Trim().Trim('"');
                RefreshToken = null;
            }
            return !string.IsNullOrEmpty(AccessToken);
        }

        private void ApplyHeaders()
        {
            if (!string.IsNullOrEmpty(AccessToken))
            {
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
            }
        }

        public async Task<Models.Player?> GetPlayerByIdAsync(int id)
        {
            ApplyHeaders();
            var response = await _http.GetAsync($"/players/{id}");
            if (!response.IsSuccessStatusCode)
                return null;
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Models.Player>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<Models.Player[]?> GetAllPlayersAsync()
        {
            ApplyHeaders();
            var response = await _http.GetAsync("/players");
            if (!response.IsSuccessStatusCode)
                return null;
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Models.Player[]>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<bool> CreatePlayerAsync(Models.Player player)
        {
            ApplyHeaders();
            var content = new StringContent(JsonSerializer.Serialize(player), Encoding.UTF8, "application/json");
            var response = await _http.PostAsync("/players", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdatePlayerAsync(Models.Player player)
        {
            ApplyHeaders();
            var content = new StringContent(JsonSerializer.Serialize(player), Encoding.UTF8, "application/json");
            var response = await _http.PutAsync($"/players/{player.Id}", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeletePlayerAsync(int id)
        {
            ApplyHeaders();
            var response = await _http.DeleteAsync($"/players/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}
