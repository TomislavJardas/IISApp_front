using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Collections.Generic;
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

            return await DeserializeAsync<Player[]>(response) ?? Array.Empty<Player>();
        }

        public async Task<Player?> GetPlayerByIdAsync(string recordId)
        {
            var response = await SendWithAutoRefreshAsync(HttpMethod.Get, $"/api/players/{Uri.EscapeDataString(recordId)}");
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await DeserializeAsync<Player>(response);
        }

        public async Task<ApiResult<Player>> CreatePlayerAsync(Player player)
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
                return new ApiResult<Player>
                {
                    Success = false,
                    StatusCode = response.StatusCode,
                    ErrorMessage = await ReadErrorMessageAsync(response)
                };
            }

            return new ApiResult<Player>
            {
                Success = true,
                StatusCode = response.StatusCode,
                Data = await DeserializeAsync<Player>(response)
            };
        }

        public async Task<ApiResult<Player>> UpdatePlayerAsync(Player player)
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
                return new ApiResult<Player>
                {
                    Success = false,
                    StatusCode = response.StatusCode,
                    ErrorMessage = await ReadErrorMessageAsync(response)
                };
            }

            return new ApiResult<Player>
            {
                Success = true,
                StatusCode = response.StatusCode,
                Data = await DeserializeAsync<Player>(response)
            };
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


        private async Task<string> ReadErrorMessageAsync(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                return $"Request failed with status code {(int)response.StatusCode} ({response.StatusCode}).";
            }

            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                foreach (var key in new[] { "message", "error", "detail" })
                {
                    if (root.ValueKind == JsonValueKind.Object &&
                        root.TryGetProperty(key, out var simpleProp) &&
                        simpleProp.ValueKind == JsonValueKind.String)
                    {
                        var value = simpleProp.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            return value;
                        }
                    }
                }

                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("errors", out var errors))
                {
                    var errorMessage = ExtractFieldErrors(errors);
                    if (!string.IsNullOrWhiteSpace(errorMessage))
                    {
                        return errorMessage;
                    }
                }

                if (root.ValueKind == JsonValueKind.Object &&
                    root.TryGetProperty("data", out var data) &&
                    data.ValueKind == JsonValueKind.Object)
                {
                    var errorMessage = ExtractFieldErrors(data);
                    if (!string.IsNullOrWhiteSpace(errorMessage))
                    {
                        return errorMessage;
                    }
                }
            }
            catch (JsonException)
            {
                return body;
            }

            return body;
        }

        private static string? ExtractFieldErrors(JsonElement element)
        {
            var messages = new List<string>();

            if (element.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            foreach (var property in element.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.String)
                {
                    var value = property.Value.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        messages.Add($"{property.Name}: {value}");
                    }

                    continue;
                }

                if (property.Value.ValueKind == JsonValueKind.Object)
                {
                    if (property.Value.TryGetProperty("message", out var messageProp) && messageProp.ValueKind == JsonValueKind.String)
                    {
                        var value = messageProp.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            messages.Add($"{property.Name}: {value}");
                        }
                    }
                }
            }

            return messages.Count > 0 ? string.Join(Environment.NewLine, messages) : null;
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
