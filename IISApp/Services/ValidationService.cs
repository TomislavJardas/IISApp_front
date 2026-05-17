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
            // The current backend supports only validate+save via /validateAndSaveXml.
            // Schema is left in the signature because the UI still lets the user choose for demo parity.
            using var content = new StringContent(xml, Encoding.UTF8, "application/xml");
            var response = await _api.HttpClient.PostAsync("/validateAndSaveXml", content);
            var body = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return body;
            }

            return ApiService.FormatErrorMessage($"Validation/save failed ({(int)response.StatusCode}): {body}");
        }
    }
}
