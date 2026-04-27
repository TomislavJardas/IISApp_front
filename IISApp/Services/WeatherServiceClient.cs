using System;
using System.Collections.Generic;
using System.Net;
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
            _http = new HttpClient
            {
                BaseAddress = new Uri(serviceUrl),
                Timeout = TimeSpan.FromSeconds(10)
            };
        }

        public async Task<IReadOnlyList<WeatherResult>> GetTemperaturesAsync(string city)
        {
            var escapedCity = SecurityElement.Escape(city) ?? string.Empty;
            var xmlRpcRequest = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<methodCall>
  <methodName>WeatherService.getTemperature</methodName>
  <params>
    <param><value><string>{escapedCity}</string></value></param>
  </params>
</methodCall>";

            HttpResponseMessage response;
            try
            {
                response = await SendWithFallbackAsync(xmlRpcRequest);
            }
            catch (TaskCanceledException ex)
            {
                throw new WeatherServiceConnectionException(
                    "Weather request timed out. The XML-RPC server at http://localhost:9090/RPC2 may not be running.", ex);
            }
            catch (HttpRequestException ex)
            {
                throw new WeatherServiceConnectionException(
                    "Cannot reach weather XML-RPC server. Ensure WeatherServer.java is running on http://localhost:9090/RPC2.", ex);
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new WeatherServiceConnectionException(
                    $"Weather XML-RPC server returned HTTP {(int)response.StatusCode}. Expected service at http://localhost:9090/RPC2.");
            }

            var xml = await response.Content.ReadAsStringAsync();
            return ParseTemperatures(xml);
        }

        private async Task<HttpResponseMessage> SendWithFallbackAsync(string payload)
        {
            var endpoints = new[] { string.Empty, "/RPC2", "/" };
            HttpResponseMessage? lastResponse = null;

            foreach (var endpoint in endpoints)
            {
                using var content = new StringContent(payload, new UTF8Encoding(false), "text/xml");
                lastResponse = await _http.PostAsync(endpoint, content);
                if (lastResponse.IsSuccessStatusCode || lastResponse.StatusCode != HttpStatusCode.NotFound)
                {
                    return lastResponse;
                }
            }

            return lastResponse ?? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        }

        private List<WeatherResult> ParseTemperatures(string xml)
        {
            var result = new List<WeatherResult>();
            var doc = new XmlDocument();
            doc.LoadXml(xml);

            var faultNode = doc.SelectSingleNode("/methodResponse/fault");
            if (faultNode != null)
            {
                var faultString = doc.SelectSingleNode("//*[local-name()='member'][*[local-name()='name' and text()='faultString']]//*[local-name()='string']")?.InnerText
                                  ?? faultNode.InnerText.Trim();
                result.Add(WeatherResult.Error($"XML-RPC fault: {faultString}"));
                return result;
            }

            var valueNodes = doc.SelectNodes("/methodResponse/params/param/value/array/data/value");
            if (valueNodes == null)
            {
                return result;
            }

            foreach (XmlNode valueNode in valueNodes)
            {
                var message = valueNode.InnerText.Trim();
                if (string.IsNullOrWhiteSpace(message))
                {
                    continue;
                }

                if (message.Contains(':'))
                {
                    var split = message.Split(':', 2, StringSplitOptions.TrimEntries);
                    result.Add(new WeatherResult(split[0], split[1], null, false));
                }
                else if (message.StartsWith("Error retrieving temperature:", StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(WeatherResult.Error(message));
                }
                else
                {
                    result.Add(new WeatherResult(string.Empty, string.Empty, message, false));
                }
            }

            return result;
        }
    }

    public class WeatherResult
    {
        public WeatherResult(string city, string temperature, string? message, bool isError)
        {
            City = city;
            Temperature = temperature;
            Message = message;
            IsError = isError;
        }

        public string City { get; }
        public string Temperature { get; }
        public string? Message { get; }
        public bool IsError { get; }

        public static WeatherResult Error(string message) => new(string.Empty, string.Empty, message, true);

        public string ToDisplayText()
        {
            if (!string.IsNullOrWhiteSpace(Message))
            {
                return Message;
            }

            return $"{City}: {Temperature} °C";
        }
    }

    public class WeatherServiceConnectionException : Exception
    {
        public WeatherServiceConnectionException(string message, Exception? inner = null) : base(message, inner)
        {
        }
    }
}
