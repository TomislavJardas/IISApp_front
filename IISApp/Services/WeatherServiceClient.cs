using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace IISApp.Services
{
    public class WeatherServiceClient
    {
        private readonly HttpClient _http;

        public WeatherServiceClient(string baseUrl)
        {
            _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        }

        public async Task<Dictionary<string, double>> GetTemperaturesAsync(string city)
        {
            string xmlRpcRequest = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<methodCall>
    <methodName>WeatherService.getTemperature</methodName>
    <params>
        <param>
            <value><string>{System.Security.SecurityElement.Escape(city)}</string></value>
        </param>
    </params>
</methodCall>";

            try
            {
                var content = new StringContent(xmlRpcRequest, new UTF8Encoding(false), "text/xml");
                var response = await _http.PostAsync("/xmlrpc", content);

                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"Server returned status code {response.StatusCode}");

                var xml = await response.Content.ReadAsStringAsync();
                return ParseTemperatures(xml);
            }
            catch (Exception ex)
            {
                // Log or handle as needed; for now, rethrow with context
                throw new ApplicationException("Failed to get temperatures from XML-RPC service.", ex);
            }
        }

        private Dictionary<string, double> ParseTemperatures(string xml)
        {
            var result = new Dictionary<string, double>();
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xml);
                var structs = doc.GetElementsByTagName("struct");
                foreach (XmlNode s in structs)
                {
                    string? name = null;
                    double temp = 0;
                    foreach (XmlNode member in s.ChildNodes)
                    {
                        if (member.Name != "member") continue;
                        var nameNode = member.SelectSingleNode("name");
                        var valueNode = member.SelectSingleNode("value");
                        if (nameNode == null || valueNode == null) continue;
                        if (nameNode.InnerText.Equals("city", StringComparison.OrdinalIgnoreCase))
                            name = valueNode.InnerText;
                        else if (nameNode.InnerText.Equals("temperature", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!double.TryParse(valueNode.InnerText, out temp))
                                temp = double.NaN; // or skip, or set to 0
                        }
                    }
                    if (!string.IsNullOrEmpty(name))
                        result[name] = temp;
                }
            }
            catch (XmlException ex)
            {
                throw new ApplicationException("Failed to parse XML response.", ex);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Unexpected error while parsing temperatures.", ex);
            }
            return result;
        }
    }
}
