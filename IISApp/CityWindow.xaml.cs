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
            _client = new WeatherServiceClient(AppConfig.WeatherGrpcAddress);
        }

        private async void SendXmlRpcRequestButton_Click(object sender, RoutedEventArgs e)
        {
            var cityName = CityNameTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(cityName))
            {
                FormattedXmlTextBox.Text = "Please enter a city name.";
                return;
            }

            try
            {
                var results = await _client.GetTemperaturesAsync(cityName);
                if (results.Count == 0)
                {
                    FormattedXmlTextBox.Text = "No weather results returned.";
                    return;
                }

                FormattedXmlTextBox.Text = string.Join(Environment.NewLine, results.Select(r => r.ToString()));
            }
            catch (Exception ex)
            {
                FormattedXmlTextBox.Text = $"Weather service error: {ex.Message}";
            }
        }
    }
}
