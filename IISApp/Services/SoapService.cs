using System;
using System.Collections.Generic;
using System.Net.Http;
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
            string soapBody = $"""
            <SOAP-ENV:Envelope xmlns:SOAP-ENV="http://schemas.xmlsoap.org/soap/envelope/" xmlns:web="http://example.com/players">
              <SOAP-ENV:Header/>
              <SOAP-ENV:Body>
                <web:SearchRequest>
                  <web:SearchTerm>{searchTerm}</web:SearchTerm>
                </web:SearchRequest>
              </SOAP-ENV:Body>
            </SOAP-ENV:Envelope>
            """;

            var content = new StringContent(soapBody, Encoding.UTF8, "text/xml");
            var response = await _http.PostAsync("/ws/players", content);
            var xml = await response.Content.ReadAsStringAsync();
            return ParsePlayers(xml);
        }

        private Player[] ParsePlayers(string xml)
        {
            var list = new List<Player>();
            var doc = new XmlDocument();
            doc.LoadXml(xml);

            // Find all elements with local-name 'Player' regardless of namespace
            var nodes = doc.SelectNodes("//*[local-name()='Player']");
            if (nodes != null)
            {
                foreach (XmlNode node in nodes)
                {
                    var player = new Player();
                    foreach (XmlNode child in node.ChildNodes)
                    {
                        switch (child.LocalName.ToLower())
                        {
                            case "id":
                                if (int.TryParse(child.InnerText, out var id))
                                    player.Id = id;
                                break;
                            case "name":
                                player.Name = child.InnerText;
                                break;
                            case "team":
                                player.Team = child.InnerText;
                                break;
                            case "season":
                                player.Season = child.InnerText;
                                break;
                            case "points":
                                if (double.TryParse(child.InnerText, out var p))
                                    player.Points = p;
                                break;
                        }
                    }
                    list.Add(player);
                }
            }
            return list.ToArray();
        }
    }
}
