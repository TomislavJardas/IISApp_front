using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using IISApp.Services;

namespace IISApp
{
    /// <summary>
    /// Interaction logic for CityWindow.xaml
    /// </summary>
    public partial class CityWindow : Window
    {
        private readonly WeatherServiceClient _client;

        public CityWindow()
        {
            InitializeComponent();
            _client = new WeatherServiceClient("http://localhost:9090");
        }

        private async void SendXmlRpcRequestButton_Click(object sender, RoutedEventArgs e)
        {
            string cityName = CityNameTextBox.Text.Trim();
            try
            {
                var results = await _client.GetTemperaturesAsync(cityName);
                ResultsListBox.ItemsSource = results.Select(kvp => $"{kvp.Key}: {kvp.Value}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
