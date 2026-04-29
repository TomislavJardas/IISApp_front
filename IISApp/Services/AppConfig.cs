using System;
using System.Configuration;

namespace IISApp.Services;

public sealed class AppConfig
{
    public string ApiBaseUrl { get; init; } = "http://localhost:8080";
    public string SoapBaseUrl { get; init; } = "http://localhost:8080/ws";
    public string WeatherServiceUrl { get; init; } = "http://localhost:9090/RPC2";
    public string RelaxNgBaseUrl { get; init; } = "http://localhost:8081";
    public FrontendAccessMode AccessMode { get; init; } = FrontendAccessMode.FullAccess;

    public static AppConfig Load()
    {
        var modeText = (ConfigurationManager.AppSettings["AccessMode"] ?? "FullAccess").Trim();
        var mode = modeText.Equals("ReadOnly", StringComparison.OrdinalIgnoreCase) ? FrontendAccessMode.ReadOnly : FrontendAccessMode.FullAccess;

        return new AppConfig
        {
            ApiBaseUrl = Read("ApiBaseUrl", "http://localhost:8080"),
            SoapBaseUrl = Read("SoapBaseUrl", "http://localhost:8080/ws"),
            WeatherServiceUrl = Read("WeatherServiceUrl", "http://localhost:9090/RPC2"),
            RelaxNgBaseUrl = Read("RelaxNgBaseUrl", "http://localhost:8081"),
            AccessMode = mode
        };
    }

    private static string Read(string key, string fallback)
    {
        var v = ConfigurationManager.AppSettings[key];
        return string.IsNullOrWhiteSpace(v) ? fallback : v.Trim();
    }
}
