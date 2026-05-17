using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Net.Client;
using IISApp.Grpc;

namespace IISApp.Services
{
    public class WeatherServiceClient
    {
        private readonly WeatherGrpcService.WeatherGrpcServiceClient _grpcClient;

        public WeatherServiceClient(string grpcAddress)
        {
            var channel = GrpcChannel.ForAddress(grpcAddress);
            _grpcClient = new WeatherGrpcService.WeatherGrpcServiceClient(channel);
        }

        public async Task<IReadOnlyList<WeatherResult>> GetTemperaturesAsync(string city)
        {
            var response = await _grpcClient.GetTemperatureAsync(new TemperatureRequest { CityName = city ?? string.Empty });
            return ParseTemperatures(response.Results);
        }

        private static IReadOnlyList<WeatherResult> ParseTemperatures(IEnumerable<string> temperatureRows)
        {
            var result = new List<WeatherResult>();

            foreach (var row in temperatureRows)
            {
                var text = (row ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                if (text.Contains(':'))
                {
                    var split = text.Split(':', 2, StringSplitOptions.TrimEntries);
                    var city = split.ElementAtOrDefault(0) ?? string.Empty;
                    var temperature = split.ElementAtOrDefault(1) ?? string.Empty;
                    result.Add(new WeatherResult(city, temperature));
                    continue;
                }

                result.Add(new WeatherResult(city: string.Empty, temperature: string.Empty, message: text));
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
