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
   
    public partial class ValidateAndSaveWindow : Window
    {
        public ValidateAndSaveWindow()
        {
            InitializeComponent();
        }
        private async void SendToValidateButton_Click(object sender, RoutedEventArgs e)
        {
            await SendRequestAsync("http://localhost:8081/validate");
        }

        private async void SendToSaveAndValidateButton_Click(object sender, RoutedEventArgs e)
        {
            await SendRequestAsync("http://localhost:8080/validateAndSaveXml");
        }

        private async Task SendRequestAsync(string url)
        {
            string countryName = CountryNameTextBox.Text;
            string subRegion = SubRegionTextBox.Text;
            string year = YearTextBox.Text;
            string value = ValueTextBox.Text;


            string xmlBody = $"""
            <Countries>
                <Country>
                    <name>{countryName}</name>
                    <subRegion>{subRegion}</subRegion>
                    <year>{year}</year>
                    <value>{value}</value>
                </Country>
            </Countries>
            """;

            try
            {
                using HttpClient client = new HttpClient();
                StringContent content = new StringContent(xmlBody, Encoding.UTF8, "application/xml");
                HttpResponseMessage response = await client.PostAsync(url, content);

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
