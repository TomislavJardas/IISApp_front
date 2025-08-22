using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security;
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
using System.Xml;

namespace IISApp
{
    /// <summary>
    /// Interaction logic for CityWindow.xaml
    /// </summary>
    public partial class CityWindow : Window
    {
        public CityWindow()
        {
            InitializeComponent();
        }
        private async void SendXmlRpcRequestButton_Click(object sender, RoutedEventArgs e)
        {
            string cityName = CityNameTextBox.Text.Trim();

            string xmlRpcRequest = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<methodCall>
    <methodName>WeatherService.getTemperature</methodName>
    <params>
        <param>
            <value><string>{SecurityElement.Escape(cityName)}</string></value>
        </param>
    </params>
</methodCall>";


            try
            {
                using HttpClient client = new HttpClient();
                StringContent content = new StringContent(xmlRpcRequest, new UTF8Encoding(false), "text/xml");

                // Send the POST request
                HttpResponseMessage response = await client.PostAsync("http://localhost:9090/xmlrpc", content);

                response.EnsureSuccessStatusCode();

                // Read and display the response as formatted XML
                string responseContent = await response.Content.ReadAsStringAsync();
                XmlRpcResponseTextBox.Text = FormatXml(responseContent);
            }
            catch (Exception ex)
            {
                XmlRpcResponseTextBox.Text = $"Error: {ex.Message}";
            }
        }

        private string FormatXml(string xml)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);

                using StringWriter sw = new StringWriter();
                using XmlTextWriter xtw = new XmlTextWriter(sw) { Formatting = Formatting.Indented };
                doc.WriteTo(xtw);

                return sw.ToString();
            }
            catch
            {
                return xml;  
            }
        }
    }
}
