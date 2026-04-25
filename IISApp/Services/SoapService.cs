using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using IISApp.Models;

namespace IISApp.Services
{
    public class SoapService
    {
        private readonly HttpClient _http;

        public SoapService(string baseUrl)
        {
            _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        }

        public async Task<Player[]> SearchPlayersAsync(string searchTerm)
        {
            var safeTerm = SecurityElement.Escape(searchTerm) ?? string.Empty;
            var soapBody = $"""
<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:pl="http://example.com/players">
  <soapenv:Header/>
  <soapenv:Body>
    <pl:SearchRequest>
      <pl:SearchTerm>{safeTerm}</pl:SearchTerm>
    </pl:SearchRequest>
  </soapenv:Body>
</soapenv:Envelope>
""";

            using var content = new StringContent(soapBody, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", "");

            var response = await _http.PostAsync("/ws", content);
            var xml = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"SOAP request failed ({(int)response.StatusCode}): {xml}");
            }

            return ParsePlayers(xml);
        }

        private static Player[] ParsePlayers(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);

            var faultNode = doc.SelectSingleNode("//*[local-name()='Fault']");
            if (faultNode != null)
            {
                var faultText = faultNode.InnerText.Trim();
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(faultText) ? "SOAP fault received." : $"SOAP fault: {faultText}");
            }

            var list = new List<Player>();
            var nodes = doc.SelectNodes("//*[local-name()='SearchResponse']/*[local-name()='Player']");
            if (nodes == null)
            {
                return Array.Empty<Player>();
            }

            foreach (XmlNode node in nodes)
            {
                var player = new Player
                {
                    Name = node.SelectSingleNode("./*[local-name()='name']")?.InnerText,
                    Team = node.SelectSingleNode("./*[local-name()='team']")?.InnerText,
                    Season = int.TryParse(node.SelectSingleNode("./*[local-name()='season']")?.InnerText, out var season) ? season : 0,
                    Points = double.TryParse(node.SelectSingleNode("./*[local-name()='points']")?.InnerText, NumberStyles.Float, CultureInfo.InvariantCulture, out var points) ? points : 0
                };

                list.Add(player);
            }

            return list.ToArray();
        }
    }
}
