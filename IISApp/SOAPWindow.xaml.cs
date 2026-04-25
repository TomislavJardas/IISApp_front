using System;
using System.Linq;
using System.Windows;
using IISApp.Services;

namespace IISApp
{
    public partial class SOAPWindow : Window
    {
        private readonly SoapService _soap;

        public SOAPWindow()
        {
            InitializeComponent();
            _soap = new SoapService(AppConfig.SoapBaseUrl);
        }

        private async void SendSOAPRequestButton_Click(object sender, RoutedEventArgs e)
        {
            var searchTerm = SearchTermTextBox.Text.Trim();
            try
            {
                var players = await _soap.SearchPlayersAsync(searchTerm);
                PlayersTextBox.Text = players.Length == 0
                    ? "No players matched the SOAP search term."
                    : string.Join(Environment.NewLine, players.Select(p => p.ToString()));
            }
            catch (Exception ex)
            {
                PlayersTextBox.Text = $"SOAP error: {ex.Message}";
            }
        }
    }
}
