using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace IISApp
{
    /// <summary>
    /// Interaction logic for CountriesWindow.xaml
    /// </summary>
    public partial class CountriesWindow : Window
    {
        private readonly string _jwtToken;

        public CountriesWindow(string jwtToken)
        {
            InitializeComponent();
            _jwtToken = jwtToken;
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string country = CountryTextBox.Text;
            string year = YearTextBox.Text;
            string requestUrl = $"http://localhost:8080/countries?country={country}&year={year}";

            try
            {
                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_jwtToken}");
                HttpResponseMessage response = await client.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();

                string responseContent = await response.Content.ReadAsStringAsync();
                ResponseTextBox.Text = responseContent;
            }
            catch (Exception ex)
            {
                ResponseTextBox.Text = $"Error: {ex.Message}";
            }
        }
    }
}

