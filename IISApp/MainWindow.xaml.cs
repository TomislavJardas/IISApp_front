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
        private readonly ValidationService _validator;

        public MainWindow()
        {
            InitializeComponent();
            _api = new ApiService("http://localhost:8080");
            _validator = new ValidationService("http://localhost:8080");
        }
        private void OpenLoginWindowButton_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow(_api, _validator);
            loginWindow.Show();
        }

        private void OpenPlayersWindowButton_Click(object sender, RoutedEventArgs e)
        {
            PlayersWindow playersWindow = new PlayersWindow(_api, _validator);
            playersWindow.Show();
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
