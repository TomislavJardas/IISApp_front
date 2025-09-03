using System;
using System.Collections.Generic;
using System.Globalization;
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
                Console.WriteLine($"XML Response: {xml}");
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

                // Try to parse struct nodes first
                var structs = doc.SelectNodes("/methodResponse/params/param/value/array/data/value/struct | /methodResponse/params/param/value/struct");
                if (structs != null && structs.Count > 0)
                {
                    foreach (XmlNode structNode in structs)
                    {
                        var cityNode = structNode.SelectSingleNode("member[name='city']/value | member[@name='city']/value");
                        var tempNode = structNode.SelectSingleNode("member[name='temperature']/value | member[@name='temperature']/value");
                        if (cityNode == null || tempNode == null)
                            continue;

                        var cityName = cityNode.InnerText;
                        if (double.TryParse(tempNode.InnerText, NumberStyles.Float, CultureInfo.InvariantCulture, out var temperature))
                        {
                            result[cityName] = temperature;
                        }
                    }
                }
                else
                {
                    // Fallback: try to parse a single value
                    var valueNode = doc.SelectSingleNode("/methodResponse/params/param/value");
                    if (valueNode != null && double.TryParse(valueNode.InnerText, NumberStyles.Float, CultureInfo.InvariantCulture, out var temperature))
                    {
                        result["Unknown"] = temperature; // Use a default key if city is not provided
                    }
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
