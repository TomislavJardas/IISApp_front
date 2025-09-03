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
            _api.ApplyHeaders();
            var url = "/validateAndSaveXml";
            var content = new StringContent(xml, Encoding.UTF8, "application/xml");
            var response = await _api.HttpClient.PostAsync(url, content);
            return await response.Content.ReadAsStringAsync();
        }
    }
}

