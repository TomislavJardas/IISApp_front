using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace IISApp.Services;

public sealed class WeatherServiceClient
{
    private readonly HttpClient _http;

    public WeatherServiceClient(string serviceUrl) => _http = new HttpClient { BaseAddress = new Uri(serviceUrl) };

    public async Task<IReadOnlyList<WeatherResult>> GetTemperaturesAsync(string city)
    {
        var escapedCity = SecurityElement.Escape(city) ?? string.Empty;
        var xmlRpcRequest = $"<?xml version=\"1.0\" encoding=\"UTF-8\"?><methodCall><methodName>WeatherService.getTemperature</methodName><params><param><value><string>{escapedCity}</string></value></param></params></methodCall>";
        using var content = new StringContent(xmlRpcRequest, new UTF8Encoding(false), "text/xml");
        var response = await _http.PostAsync(string.Empty, content);
        var xml = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Weather service returned {(int)response.StatusCode}: {xml}");
        }

        return ParseTemperatures(xml);
    }

    public static List<WeatherResult> ParseTemperatures(string xml)
    {
        var result = new List<WeatherResult>();
        var doc = new XmlDocument();
        doc.LoadXml(xml);

        var fault = doc.SelectSingleNode("/methodResponse/fault//member[name='faultString']/value/string");
        if (fault != null)
        {
            result.Add(new WeatherResult("", "", fault.InnerText, true));
            return result;
        }

        var valueNodes = doc.SelectNodes("/methodResponse/params/param/value/array/data/value");
        if (valueNodes == null) return result;

        foreach (XmlNode node in valueNodes)
        {
            var text = node.InnerText.Trim();
            if (string.IsNullOrWhiteSpace(text)) continue;
            if (text.Contains(':'))
            {
                var parts = text.Split(':', 2, StringSplitOptions.TrimEntries);
                result.Add(new WeatherResult(parts[0], parts[1], null, false));
            }
            else
            {
                result.Add(new WeatherResult("", "", text, text.StartsWith("Error", StringComparison.OrdinalIgnoreCase)));
            }
        }

        return result;
    }
}

public sealed class WeatherResult
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
}
