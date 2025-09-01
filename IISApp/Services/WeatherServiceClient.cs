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
            string xmlRpcRequest = $@"<?xml version=\"1.0\" encoding=\"UTF-8\"?>
<methodCall>
    <methodName>WeatherService.getTemperature</methodName>
    <params>
        <param>
            <value><string>{System.Security.SecurityElement.Escape(city)}</string></value>
        </param>
    </params>
</methodCall>";

            var content = new StringContent(xmlRpcRequest, new UTF8Encoding(false), "text/xml");
            var response = await _http.PostAsync("/xmlrpc", content);
            var xml = await response.Content.ReadAsStringAsync();
            return ParseTemperatures(xml);
        }

        private Dictionary<string, double> ParseTemperatures(string xml)
        {
            var result = new Dictionary<string, double>();
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
                    if (nameNode.InnerText.ToLower() == "city")
                        name = valueNode.InnerText;
                    else if (nameNode.InnerText.ToLower() == "temperature" && double.TryParse(valueNode.InnerText, out var t))
                        temp = t;
                }
                if (!string.IsNullOrEmpty(name))
                    result[name] = temp;
            }
            return result;
        }
    }
}
