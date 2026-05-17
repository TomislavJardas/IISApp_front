using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Collections.Generic;
using System.Text.Json;
using System.Globalization;
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

        public async Task<ApiResult<Player>> CreatePlayerFromRawAsync(string name, string team, string seasonText, string pointsText)
        {
            var payload = BuildRawPlayerPayload(name, team, seasonText, pointsText);
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

        public async Task<ApiResult<Player>> UpdatePlayerFromRawAsync(string id, string name, string team, string seasonText, string pointsText)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Cannot update player without record id.", nameof(id));
            }

            var payload = BuildRawPlayerPayload(name, team, seasonText, pointsText);
            var response = await SendWithAutoRefreshAsync(HttpMethod.Patch, $"/api/players/{Uri.EscapeDataString(id)}", payload);
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

            return FormatErrorMessage(body);
        }

        public static string FormatErrorMessage(string rawError)
        {
            if (string.IsNullOrWhiteSpace(rawError))
            {
                return rawError;
            }

            var trimmedError = rawError.Trim();
            var jsonStartIndex = trimmedError.IndexOf('{');
            var prefix = string.Empty;
            var jsonCandidate = trimmedError;

            if (jsonStartIndex > 0)
            {
                prefix = trimmedError[..jsonStartIndex].Trim().TrimEnd(':');
                var markerIndex = prefix.IndexOf(" (", StringComparison.Ordinal);
                if (markerIndex > 0)
                {
                    prefix = prefix[..markerIndex];
                }

                if (!prefix.EndsWith(".", StringComparison.Ordinal))
                {
                    prefix = $"{prefix}.";
                }

                jsonCandidate = trimmedError[jsonStartIndex..];
            }

            try
            {
                using var doc = JsonDocument.Parse(jsonCandidate);
                var root = doc.RootElement;

                var lines = new List<string>();
                if (!string.IsNullOrWhiteSpace(prefix))
                {
                    lines.Add(prefix);
                }

                if (root.ValueKind == JsonValueKind.Object &&
                    root.TryGetProperty("status", out var statusProp) &&
                    (statusProp.ValueKind == JsonValueKind.Number || statusProp.ValueKind == JsonValueKind.String))
                {
                    var statusValue = statusProp.ToString();
                    if (!string.IsNullOrWhiteSpace(statusValue))
                    {
                        lines.Add($"Status: {statusValue}");
                    }
                }

                if (root.ValueKind == JsonValueKind.Object &&
                    root.TryGetProperty("error", out var errorProp) &&
                    errorProp.ValueKind == JsonValueKind.String)
                {
                    var errorValue = errorProp.GetString();
                    if (!string.IsNullOrWhiteSpace(errorValue))
                    {
                        lines.Add($"Error: {errorValue}");
                    }
                }

                if (root.ValueKind == JsonValueKind.Object &&
                    root.TryGetProperty("path", out var pathProp) &&
                    pathProp.ValueKind == JsonValueKind.String)
                {
                    var pathValue = pathProp.GetString();
                    if (!string.IsNullOrWhiteSpace(pathValue))
                    {
                        lines.Add($"Path: {pathValue}");
                    }
                }

                string? generalMessage = null;
                foreach (var key in new[] { "message", "detail" })
                {
                    if (root.ValueKind == JsonValueKind.Object &&
                        root.TryGetProperty(key, out var simpleProp) &&
                        simpleProp.ValueKind == JsonValueKind.String)
                    {
                        var value = simpleProp.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            generalMessage = value;
                            break;
                        }
                    }
                }

                var fieldMessages = new List<string>();
                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("errors", out var errors))
                {
                    var errorMessage = ExtractFieldErrors(errors);
                    if (!string.IsNullOrWhiteSpace(errorMessage))
                    {
                        fieldMessages.Add(errorMessage);
                    }
                }

                if (root.ValueKind == JsonValueKind.Object &&
                    root.TryGetProperty("data", out var data) &&
                    data.ValueKind == JsonValueKind.Object)
                {
                    var errorMessage = ExtractFieldErrors(data);
                    if (!string.IsNullOrWhiteSpace(errorMessage))
                    {
                        fieldMessages.Add(errorMessage);
                    }
                }

                if (!string.IsNullOrWhiteSpace(generalMessage) && fieldMessages.Count > 0)
                {
                    return $"{generalMessage}{Environment.NewLine}{string.Join(Environment.NewLine, fieldMessages)}";
                }

                if (!string.IsNullOrWhiteSpace(generalMessage) && lines.Count > 0)
                {
                    lines.Insert(0, generalMessage);
                    return string.Join(Environment.NewLine, lines);
                }

                if (fieldMessages.Count > 0 && lines.Count > 0)
                {
                    lines.Add(string.Join(Environment.NewLine, fieldMessages));
                    return string.Join(Environment.NewLine, lines);
                }

                if (lines.Count > 0)
                {
                    return string.Join(Environment.NewLine, lines);
                }

                if (!string.IsNullOrWhiteSpace(generalMessage))
                {
                    return generalMessage;
                }

                if (fieldMessages.Count > 0)
                {
                    return string.Join(Environment.NewLine, fieldMessages);
                }
            }
            catch (JsonException)
            {
                return rawError;
            }

            return rawError;
        }

        private static Dictionary<string, object> BuildRawPlayerPayload(string name, string team, string seasonText, string pointsText)
        {
            return new Dictionary<string, object>
            {
                ["name"] = name,
                ["team"] = team,
                ["season"] = int.TryParse(seasonText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var season)
                    ? season
                    : seasonText,
                ["points"] = double.TryParse(pointsText, NumberStyles.Float, CultureInfo.InvariantCulture, out var points)
                    ? points
                    : pointsText
            };
        }

        private static string? ExtractFieldErrors(JsonElement element)
        {
            var messages = new List<string>();

            if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        var value = item.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            messages.Add($"- {value}");
                        }
                    }
                }

                return messages.Count > 0 ? string.Join(Environment.NewLine, messages) : null;
            }

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
