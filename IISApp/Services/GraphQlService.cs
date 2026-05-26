using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using IISApp.Models;

namespace IISApp.Services
{
    public class GraphQlService
    {
        private readonly ApiService _api;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public GraphQlService(ApiService api)
        {
            _api = api;
        }

        public Task<ApiResult<Player[]>> GetPlayersAsync()
            => SendAsync<Player[]>("query { players { recordId name team season points } }", null, "players");

        public Task<ApiResult<Player>> GetPlayerByIdAsync(string recordId)
            => SendAsync<Player>(
                "query($recordId: ID!) { playerById(recordId: $recordId) { recordId name team season points } }",
                new { recordId },
                "playerById");

        public Task<ApiResult<Player>> CreatePlayerAsync(string name, string team, int season, double points)
            => SendAsync<Player>(
                "mutation($input: PlayerInput!) { createPlayer(input: $input) { recordId name team season points } }",
                new { input = new { name, team, season, points } },
                "createPlayer");

        public Task<ApiResult<Player>> UpdatePlayerAsync(string recordId, string name, string team, int season, double points)
            => SendAsync<Player>(
                "mutation($recordId: ID!, $input: PlayerInput!) { updatePlayer(recordId: $recordId, input: $input) { recordId name team season points } }",
                new { recordId, input = new { name, team, season, points } },
                "updatePlayer");

        public Task<ApiResult<bool>> DeletePlayerAsync(string recordId)
            => SendAsync<bool>(
                "mutation($recordId: ID!) { deletePlayer(recordId: $recordId) }",
                new { recordId },
                "deletePlayer");

        private async Task<ApiResult<T>> SendAsync<T>(string query, object? variables, string dataField)
        {
            try
            {
                var payload = new { query, variables = variables ?? new { } };
                using var request = new HttpRequestMessage(HttpMethod.Post, "/graphql")
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                };

                var response = await _api.HttpClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return Failed<T>(ApiService.FormatErrorMessage(body));
                }

                if (string.IsNullOrWhiteSpace(body))
                {
                    return Failed<T>("GraphQL server returned an empty response.");
                }

                GraphQlEnvelope? envelope;
                try
                {
                    envelope = JsonSerializer.Deserialize<GraphQlEnvelope>(body, _jsonOptions);
                }
                catch (JsonException)
                {
                    return Failed<T>("Could not parse GraphQL response.");
                }

                if (envelope is null)
                {
                    return Failed<T>("GraphQL response was empty or malformed.");
                }

                if (envelope.Errors is { Count: > 0 })
                {
                    return Failed<T>(FormatGraphQlErrors(envelope.Errors));
                }

                if (envelope.Data.ValueKind != JsonValueKind.Object || !envelope.Data.TryGetProperty(dataField, out var dataElement))
                {
                    return Failed<T>("GraphQL response did not contain expected data.");
                }

                if (dataElement.ValueKind == JsonValueKind.Null)
                {
                    return new ApiResult<T> { Success = true, Data = default };
                }

                T? data;
                try
                {
                    data = dataElement.Deserialize<T>(_jsonOptions);
                }
                catch (JsonException)
                {
                    return Failed<T>("GraphQL data format was not valid.");
                }

                return new ApiResult<T> { Success = true, Data = data };
            }
            catch (Exception ex)
            {
                return Failed<T>($"GraphQL request failed: {ex.Message}");
            }
        }

        private static ApiResult<T> Failed<T>(string message) => new()
        {
            Success = false,
            ErrorMessage = string.IsNullOrWhiteSpace(message) ? "GraphQL request failed." : message
        };

        private static string FormatGraphQlErrors(IReadOnlyList<GraphQlError> errors)
        {
            var parts = new List<string>();
            foreach (var error in errors)
            {
                if (!string.IsNullOrWhiteSpace(error.Message))
                {
                    parts.Add(error.Message.Trim());
                }
            }

            return parts.Count > 0
                ? string.Join(Environment.NewLine, parts)
                : "GraphQL request returned one or more errors.";
        }

        private class GraphQlEnvelope
        {
            public JsonElement Data { get; set; }
            public List<GraphQlError>? Errors { get; set; }
        }

        private class GraphQlError
        {
            public string? Message { get; set; }
            [JsonPropertyName("extensions")]
            public JsonElement Extensions { get; set; }
        }
    }
}
