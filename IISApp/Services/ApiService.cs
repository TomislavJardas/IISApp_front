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

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            AccessToken = doc.RootElement.GetProperty("accessToken").GetString();
            RefreshToken = doc.RootElement.TryGetProperty("refreshToken", out var refresh) ? refresh.GetString() : null;
            return !string.IsNullOrEmpty(AccessToken);
        }

        private void ApplyHeaders()
        {
            if (!string.IsNullOrEmpty(AccessToken))
            {
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
            }
        }

        public async Task<string> GetCountriesAsync(string country, string year)
        {
            ApplyHeaders();
            var url = $"/countries?country={country}&year={year}";
            return await _http.GetStringAsync(url);
        }
    }
}
