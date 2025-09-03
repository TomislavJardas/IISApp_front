using System;
using System.Windows;
using IISApp.Services;

namespace IISApp
{
    /// <summary>
    /// Interaction logic for SOAPWindow.xaml
    /// </summary>
    public partial class SOAPWindow : Window
    {
        private readonly SoapService _soap;

        public SOAPWindow()
        {
            InitializeComponent();
            _soap = new SoapService("http://localhost:8080");
        }

        private async void SendSOAPRequestButton_Click(object sender, RoutedEventArgs e)
        {
            string searchTerm = SearchTermTextBox.Text;
            try
            {
                var players = await _soap.SearchPlayersAsync(searchTerm);
                // FIX: Use p.ToString() instead of p.ToString to get the string representation.
                PlayersTextBox.Text = string.Join(Environment.NewLine, players.Select(p => p.ToString()));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}

