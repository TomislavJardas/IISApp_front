using System;
using System.Windows;
using IISApp.Services;

namespace IISApp
{
    /// <summary>
    /// Interaction logic for CountriesWindow.xaml
    /// </summary>
    public partial class CountriesWindow : Window
    {
        private readonly ApiService _api;

        public CountriesWindow(ApiService api)
        {
            InitializeComponent();
            _api = api;
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string country = CountryTextBox.Text;
            string year = YearTextBox.Text;

            try
            {
                string response = await _api.GetCountriesAsync(country, year);
                ResponseTextBox.Text = response;
            }
            catch (Exception ex)
            {
                ResponseTextBox.Text = $"Error: {ex.Message}";
            }
        }
    }
}
