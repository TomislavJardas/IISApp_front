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
    /// Interaction logic for SOAPWindow.xaml
    /// </summary>
    public partial class SOAPWindow : Window
    {
        public SOAPWindow()
        {
            InitializeComponent();
        }
        private async void SendSOAPRequestButton_Click(object sender, RoutedEventArgs e)
        {
            string searchTerm = SearchTermTextBox.Text;

 
            string soapBody = $"""
            <SOAP-ENV:Envelope xmlns:SOAP-ENV="http://schemas.xmlsoap.org/soap/envelope/"
                               xmlns:web="http://example.com/countries">
               <SOAP-ENV:Header/>
               <SOAP-ENV:Body>
                  <web:SearchRequest>
                     <web:SearchTerm>{searchTerm}</web:SearchTerm>
                  </web:SearchRequest>
               </SOAP-ENV:Body>
            </SOAP-ENV:Envelope>
            """;

 
            try
            {
                using HttpClient client = new HttpClient();
                StringContent content = new StringContent(soapBody, Encoding.UTF8, "text/xml");
                HttpResponseMessage response = await client.PostAsync("http://localhost:8080/ws/countries", content);

                string responseContent = await response.Content.ReadAsStringAsync();
                SOAPResponseTextBox.Text = responseContent;
            }
            catch (Exception ex)
            {
                SOAPResponseTextBox.Text = $"Error: {ex.Message}";
            }
        }
    }
}

