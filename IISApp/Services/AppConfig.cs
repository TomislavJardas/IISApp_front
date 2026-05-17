using System;
using System.Configuration;

namespace IISApp.Services
{
    public static class AppConfig
    {
        public static string ApiBaseUrl => ReadRequired("ApiBaseUrl", "http://localhost:8080");

        public static string SoapBaseUrl => ReadRequired("SoapBaseUrl", ApiBaseUrl);

        public static string WeatherGrpcAddress => ReadRequired("WeatherGrpcAddress", "http://localhost:9090");

        public static FrontendAccessMode AccessMode
        {
            get
            {
                var value = (ConfigurationManager.AppSettings["FrontendAccessMode"] ?? "FullAccess").Trim();
                return value.Equals("ReadOnly", StringComparison.OrdinalIgnoreCase)
                    ? FrontendAccessMode.ReadOnly
                    : FrontendAccessMode.FullAccess;
            }
        }

        private static string ReadRequired(string key, string fallback)
        {
            var value = ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }
}
