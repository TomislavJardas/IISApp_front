using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using IISApp.Services;
using System.Xml;
using System.IO;

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
                // Convert Dictionary<string, double> to formatted XML string
                string formattedXml = FormatDictionaryAsXml(results);
                FormattedXmlTextBox.Text = formattedXml;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // Helper method to convert Dictionary<string, double> to XML string
        private string FormatDictionaryAsXml(System.Collections.Generic.Dictionary<string, double> dict)
        {
            var doc = new XmlDocument();
            var root = doc.CreateElement("Temperatures");
            doc.AppendChild(root);

            foreach (var kvp in dict)
            {
                var cityElem = doc.CreateElement("City");
                cityElem.InnerText = kvp.Value.ToString();
                root.AppendChild(cityElem);
            }

            using (var stringWriter = new StringWriter())
            using (var xmlTextWriter = new XmlTextWriter(stringWriter) { Formatting = Formatting.Indented })
            {
                doc.WriteTo(xmlTextWriter);
                return stringWriter.ToString();
            }
        }

    }
}
