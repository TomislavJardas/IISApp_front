using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using IISApp.Models;

namespace IISApp.Services
{
    public class ApiService
    {
        private readonly HttpClient _http;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ApiService(string baseUrl)
        {
            _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        }

        public HttpClient HttpClient => _http;

        public string? AccessToken { get; private set; }
        public string? RefreshToken { get; private set; }

        public bool IsAuthenticated => !string.IsNullOrWhiteSpace(AccessToken);

        public event Action? SessionExpired;

        public async Task<bool> LoginAsync(string username, string password)
        {
            var payload = new { username, password };
            var response = await SendAsync(HttpMethod.Post, "/api/auth/login", payload, requiresAuth: false);

            if (!response.IsSuccessStatusCode)
            {
                Logout();
                return false;
            }

            var tokenResponse = await DeserializeAsync<TokenResponse>(response);
            if (tokenResponse is null || string.IsNullOrWhiteSpace(tokenResponse.AccessToken) || string.IsNullOrWhiteSpace(tokenResponse.RefreshToken))
            {
                Logout();
                return false;
            }

            AccessToken = tokenResponse.AccessToken;
            RefreshToken = tokenResponse.RefreshToken;
            ApplyAuthorizationHeader();
            return true;
        }

        public void Logout()
        {
            AccessToken = null;
            RefreshToken = null;
            _http.DefaultRequestHeaders.Authorization = null;
        }

        public async Task<Player[]?> GetAllPlayersAsync()
        {
            var response = await SendWithAutoRefreshAsync(HttpMethod.Get, "/api/players");
            if (!response.IsSuccessStatusCode)
            {
                return Array.Empty<Player>();
            }

            return await DeserializePlayersAsync(response);
        }

        public async Task<Player?> GetPlayerByIdAsync(string recordId)
        {
            var response = await SendWithAutoRefreshAsync(HttpMethod.Get, $"/api/players/{Uri.EscapeDataString(recordId)}");
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await DeserializePlayerAsync(response);
        }

        public async Task<Player?> CreatePlayerAsync(Player player)
        {
            var payload = new
            {
                name = player.Name,
                team = player.Team,
                season = player.Season,
                points = player.Points
            };
            var response = await SendWithAutoRefreshAsync(HttpMethod.Post, "/api/players", payload);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await DeserializePlayerAsync(response);
        }

        public async Task<Player?> UpdatePlayerAsync(Player player)
        {
            if (string.IsNullOrWhiteSpace(player.Id))
            {
                throw new ArgumentException("Cannot update player without record id.", nameof(player));
            }

            var payload = new
            {
                name = player.Name,
                team = player.Team,
                season = player.Season,
                points = player.Points
            };
            var response = await SendWithAutoRefreshAsync(HttpMethod.Patch, $"/api/players/{Uri.EscapeDataString(player.Id)}", payload);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await DeserializePlayerAsync(response);
        }

        public async Task<bool> DeletePlayerAsync(string recordId)
        {
            var response = await SendWithAutoRefreshAsync(HttpMethod.Delete, $"/api/players/{Uri.EscapeDataString(recordId)}");
            return response.StatusCode == HttpStatusCode.NoContent || response.IsSuccessStatusCode;
        }

        private async Task<HttpResponseMessage> SendWithAutoRefreshAsync(HttpMethod method, string url, object? payload = null)
        {
            ApplyAuthorizationHeader();
            var response = await SendAsync(method, url, payload);
            if (response.StatusCode != HttpStatusCode.Unauthorized)
            {
                return response;
            }

            var refreshed = await TryRefreshTokenAsync();
            if (!refreshed)
            {
                Logout();
                SessionExpired?.Invoke();
                return response;
            }

            return await SendAsync(method, url, payload);
        }

        private async Task<bool> TryRefreshTokenAsync()
        {
            if (string.IsNullOrWhiteSpace(RefreshToken))
            {
                return false;
            }

            var refreshPayload = new { refreshToken = RefreshToken };
            var response = await SendAsync(HttpMethod.Post, "/api/auth/refresh", refreshPayload, requiresAuth: false);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var refreshedTokens = await DeserializeAsync<TokenResponse>(response);
            if (refreshedTokens is null || string.IsNullOrWhiteSpace(refreshedTokens.AccessToken))
            {
                return false;
            }

            AccessToken = refreshedTokens.AccessToken;
            RefreshToken = refreshedTokens.RefreshToken;
            ApplyAuthorizationHeader();
            return true;
        }

        private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? payload = null, bool requiresAuth = true)
        {
            using var request = new HttpRequestMessage(method, url);
            if (payload is not null)
            {
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            }

            if (requiresAuth && !string.IsNullOrWhiteSpace(AccessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
            }

            return await _http.SendAsync(request);
        }

        private async Task<Player[]?> DeserializePlayersAsync(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                return Array.Empty<Player>();
            }

            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<Player>();
            }

            var players = new List<Player>();
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                players.Add(MapPlayer(item));
            }

            return players.ToArray();
        }

        private async Task<Player?> DeserializePlayerAsync(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                return null;
            }

            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.ValueKind == JsonValueKind.Object ? MapPlayer(doc.RootElement) : null;
        }

        private static Player MapPlayer(JsonElement element)
        {
            return new Player
            {
                Id = ReadStringId(element),
                Name = ReadString(element, "name"),
                Team = ReadString(element, "team"),
                Season = ReadInt(element, "season"),
                Points = ReadDouble(element, "points")
            };
        }

        private static string? ReadStringId(JsonElement element)
        {
            if (element.TryGetProperty("id", out var idProp))
            {
                return ValueToString(idProp);
            }

            if (element.TryGetProperty("recordId", out var recordIdProp))
            {
                return ValueToString(recordIdProp);
            }

            return null;
        }

        private static string? ReadString(JsonElement element, string name)
        {
            return element.TryGetProperty(name, out var prop) ? ValueToString(prop) : null;
        }

        private static int ReadInt(JsonElement element, string name)
        {
            if (!element.TryGetProperty(name, out var prop))
            {
                return 0;
            }

            if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var val))
            {
                return val;
            }

            return int.TryParse(ValueToString(prop), out var parsed) ? parsed : 0;
        }

        private static double ReadDouble(JsonElement element, string name)
        {
            if (!element.TryGetProperty(name, out var prop))
            {
                return 0;
            }

            if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDouble(out var val))
            {
                return val;
            }

            return double.TryParse(ValueToString(prop), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : 0;
        }

        private static string? ValueToString(JsonElement prop)
        {
            return prop.ValueKind switch
            {
                JsonValueKind.String => prop.GetString(),
                JsonValueKind.Number => prop.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => null
            };
        }

        private async Task<T?> DeserializeAsync<T>(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(body, _jsonOptions);
        }

        private void ApplyAuthorizationHeader()
        {
            _http.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(AccessToken)
                ? null
                : new AuthenticationHeaderValue("Bearer", AccessToken);
        }

        private class TokenResponse
        {
            public string? AccessToken { get; set; }
            public string? RefreshToken { get; set; }
        }
    }
}
