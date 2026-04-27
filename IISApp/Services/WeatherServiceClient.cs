using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace IISApp.Services
{
    public class WeatherServiceClient
    {
        private readonly HttpClient _http;

        public WeatherServiceClient(string serviceUrl)
        {
            _http = new HttpClient { BaseAddress = new Uri(serviceUrl) };
        }

        public async Task<IReadOnlyList<WeatherResult>> GetTemperaturesAsync(string city)
        {
            var escapedCity = SecurityElement.Escape(city) ?? string.Empty;
            var xmlRpcRequest = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<methodCall>
    <methodName>WeatherService.getTemperature</methodName>
    <params>
        <param>
            <value><string>{escapedCity}</string></value>
        </param>
    </params>
</methodCall>";

            using var content = new StringContent(xmlRpcRequest, new UTF8Encoding(false), "text/xml");
            var response = await _http.PostAsync(string.Empty, content);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Weather service returned status {(int)response.StatusCode}.");
            }

            var xml = await response.Content.ReadAsStringAsync();
            return ParseTemperatures(xml);
        }

        private List<WeatherResult> ParseTemperatures(string xml)
        {
            var result = new List<WeatherResult>();
            var doc = new XmlDocument();
            doc.LoadXml(xml);

            var valueNodes = doc.SelectNodes("/methodResponse/params/param/value/array/data/value");
            if (valueNodes == null || valueNodes.Count == 0)
            {
                return result;
            }

            foreach (XmlNode valueNode in valueNodes)
            {
                var text = valueNode.InnerText.Trim();
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                if (text.Contains(':'))
                {
                    var split = text.Split(':', 2, StringSplitOptions.TrimEntries);
                    result.Add(new WeatherResult(split[0], split[1]));
                }
                else if (text.Equals("City not found", StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(new WeatherResult(city: string.Empty, temperature: string.Empty, message: text));
                }
                else
                {
                    result.Add(new WeatherResult(city: string.Empty, temperature: string.Empty, message: text));
                }
            }

            return result;
        }
    }

    public class WeatherResult
    {
        public WeatherResult(string city, string temperature, string? message = null)
        {
            City = city;
            Temperature = temperature;
            Message = message;
        }

        public string City { get; }
        public string Temperature { get; }
        public string? Message { get; }

        public override string ToString()
        {
            if (!string.IsNullOrWhiteSpace(Message))
            {
                return Message;
            }

            return $"{City}: {Temperature} °C";
        }
    }
}
