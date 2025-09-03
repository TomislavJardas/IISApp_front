using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Linq;

namespace IISApp.Services
{
    public class ApiService
    {
        private readonly HttpClient _http;
        public HttpClient HttpClient => _http;

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

            // Try to detect if the response is JSON
            if (!string.IsNullOrWhiteSpace(body) && (body.TrimStart().StartsWith("{") || body.TrimStart().StartsWith("[")))
            {
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    AccessToken = doc.RootElement.GetProperty("accessToken").GetString();
                    RefreshToken = doc.RootElement.TryGetProperty("refreshToken", out var refresh) ? refresh.GetString() : null;
                }
                catch (JsonException)
                {
                    AccessToken = null;
                    RefreshToken = null;
                    return false;
                }
            }
            else
            {
                // Treat as plain JWT string
                AccessToken = body.Trim().Trim('"');
                RefreshToken = null;
            }
            return !string.IsNullOrEmpty(AccessToken);
        }

        public void ApplyHeaders()
        {
            if (!string.IsNullOrEmpty(AccessToken))
            {
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
            }
        }

        public async Task<Models.Player?> GetPlayerByIdAsync(int id)
        {
            ApplyHeaders();
            var response = await _http.GetAsync($"/api/players/{id}");
            if (!response.IsSuccessStatusCode)
                return null;

            var xml = await response.Content.ReadAsStringAsync();
            try
            {
                var doc = XDocument.Parse(xml);
                var playerElement = doc.Descendants("Player").FirstOrDefault();
                if (playerElement == null)
                    return null;

                return new Models.Player
                {
                    Id = (int?)playerElement.Element("id") ?? (int?)playerElement.Element("Id") ?? 0,
                    Name = (string?)playerElement.Element("name") ?? (string?)playerElement.Element("Name"),
                    Team = (string?)playerElement.Element("team") ?? (string?)playerElement.Element("Team"),
                    Season = (string?)playerElement.Element("season") ?? (string?)playerElement.Element("Season"),
                    Points = (double?)playerElement.Element("points") ?? (double?)playerElement.Element("Points") ?? 0
                };
            }
            catch
            {
                return null;
            }
        }

        public async Task<Models.Player[]?> GetAllPlayersAsync()
        {
            ApplyHeaders();
            var response = await _http.GetAsync("/api/players");
            if (!response.IsSuccessStatusCode)
                return Array.Empty<Models.Player>();

            var xml = await response.Content.ReadAsStringAsync();
            try
            {
                var doc = XDocument.Parse(xml);
                var players = doc.Descendants("Player").Select(p => new Models.Player
                {
                    Id = (int?)p.Element("id") ?? (int?)p.Element("Id") ?? 0,
                    Name = (string?)p.Element("name") ?? (string?)p.Element("Name"),
                    Team = (string?)p.Element("team") ?? (string?)p.Element("Team"),
                    Season = (string?)p.Element("season") ?? (string?)p.Element("Season"),
                    Points = (double?)p.Element("points") ?? (double?)p.Element("Points") ?? 0
                }).ToArray();
                return players;
            }
            catch
            {
                return Array.Empty<Models.Player>();
            }
        }

        public async Task<bool> CreatePlayerAsync(Models.Player player)
        {
            ApplyHeaders();
            var content = new StringContent(JsonSerializer.Serialize(player), Encoding.UTF8, "application/json");
            var response = await _http.PostAsync("/api/players", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdatePlayerAsync(Models.Player player)
        {
            ApplyHeaders();
            var content = new StringContent(JsonSerializer.Serialize(player), Encoding.UTF8, "application/json");
            var response = await _http.PutAsync($"/api/players/{player.Id}", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeletePlayerAsync(int id)
        {
            ApplyHeaders();
            var response = await _http.DeleteAsync($"/api/players/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}
