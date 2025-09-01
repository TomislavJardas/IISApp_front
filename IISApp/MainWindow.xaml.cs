using System.Windows;
using IISApp.Services;

namespace IISApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ApiService _api;

        public MainWindow()
        {
            InitializeComponent();
            _api = new ApiService("http://localhost:8080");
        }
        private void OpenLoginWindowButton_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow(_api);
            loginWindow.Show();
        }

        private void OpenCountriesWindowButton_Click(object sender, RoutedEventArgs e)
        {
            ValidateAndSaveWindow countriesWindow = new ValidateAndSaveWindow();
            countriesWindow.Show();
        }

        private void OpenSOAPWindowButton_Click(object sender, RoutedEventArgs e)
        {
            SOAPWindow soapWindow = new SOAPWindow();
            soapWindow.Show();
        }
        private void OpenXRCPWindowButton_Click(object sender, RoutedEventArgs e)
        {
            CityWindow soapWindow = new CityWindow();
            soapWindow.Show();
        }
    }
}
