using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace IISApp.Services
{
    public class ValidationService
    {
        private readonly ApiService _api;

        public ValidationService(ApiService api)
        {
            _api = api;
        }

        public async Task<string> ValidateAndSaveAsync(string xml, string schema)
        {
            using var content = new StringContent(xml, Encoding.UTF8, "application/xml");
            var requestUri = $"/validateAndSaveXml?schema={System.Uri.EscapeDataString(schema)}";
            var response = await _api.HttpClient.PostAsync(requestUri, content);
            var body = await response.Content.ReadAsStringAsync();
            return response.IsSuccessStatusCode ? body : $"Validation/save failed ({(int)response.StatusCode}): {body}";
        }
    }
}
