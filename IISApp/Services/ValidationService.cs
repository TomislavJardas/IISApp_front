using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace IISApp.Services
{
    public class ValidationService
    {
        private readonly HttpClient _http;

        public ValidationService(string baseUrl)
        {
            _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        }

        public async Task<string> ValidateAsync(string xml, string schema)
        {
            var url = $"/validate?schema={schema}";
            var content = new StringContent(xml, Encoding.UTF8, "application/xml");
            var response = await _http.PostAsync(url, content);
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> ValidateAndSaveAsync(string xml, string schema)
        {
            var url = $"/validateAndSaveXml?schema={schema}";
            var content = new StringContent(xml, Encoding.UTF8, "application/xml");
            var response = await _http.PostAsync(url, content);
            return await response.Content.ReadAsStringAsync();
        }
    }
}
