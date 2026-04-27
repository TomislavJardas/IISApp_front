using System;
using System.Linq;
using System.Windows;
using IISApp.Services;

namespace IISApp
{
    public partial class CityWindow : Window
    {
        private readonly WeatherServiceClient _client;

        public CityWindow()
        {
            InitializeComponent();
            _client = new WeatherServiceClient(AppConfig.WeatherServiceUrl);
            StatusTextBlock.Text = $"Weather server URL: {AppConfig.WeatherServiceUrl}";
        }

        private async void SendXmlRpcRequestButton_Click(object sender, RoutedEventArgs e)
        {
            var cityName = CityNameTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(cityName))
            {
                StatusTextBlock.Text = "Please enter a city name.";
                FormattedXmlTextBox.Text = string.Empty;
                return;
            }

            SendXmlRpcRequestButton.IsEnabled = false;
            StatusTextBlock.Text = "Loading weather data...";

            try
            {
                var results = await _client.GetTemperaturesAsync(cityName);
                if (results.Count == 0)
                {
                    StatusTextBlock.Text = "No weather results returned.";
                    FormattedXmlTextBox.Text = "No data was returned by the weather service.";
                    return;
                }

                FormattedXmlTextBox.Text = string.Join(Environment.NewLine, results.Select(r => r.ToDisplayText()));
                StatusTextBlock.Text = results.Any(r => r.IsError)
                    ? "Weather service responded with an error message."
                    : $"Received {results.Count} result(s).";
            }
            catch (WeatherServiceConnectionException ex)
            {
                StatusTextBlock.Text = "Cannot connect to weather XML-RPC service.";
                FormattedXmlTextBox.Text = $"{ex.Message}{Environment.NewLine}{Environment.NewLine}" +
                                           "Tip: Start WeatherServer.java separately (it is not started by Spring Boot).";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "Unexpected weather client error.";
                FormattedXmlTextBox.Text = ex.Message;
            }
            finally
            {
                SendXmlRpcRequestButton.IsEnabled = true;
            }
        }
    }
}
