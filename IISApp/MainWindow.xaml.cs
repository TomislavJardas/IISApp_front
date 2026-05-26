using System.Windows;
using IISApp.Services;

namespace IISApp
{
    public partial class MainWindow : Window
    {
        private readonly ApiService _api;
        private readonly ValidationService _validator;
        private readonly PermissionService _permissionService;

        public MainWindow()
        {
            InitializeComponent();
            _api = new ApiService(AppConfig.ApiBaseUrl);
            _validator = new ValidationService(_api);
            _permissionService = PermissionService.FromConfiguration();
        }

        private void OpenLoginWindowButton_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginWindow(_api, _validator, _permissionService);
            loginWindow.Show();
        }

        private void OpenPlayersWindowButton_Click(object sender, RoutedEventArgs e)
        {
            var playersWindow = new PlayersWindow(_api, _validator, _permissionService);
            playersWindow.Show();
        }

        private void OpenGraphQlWindowButton_Click(object sender, RoutedEventArgs e)
        {
            var graphQlWindow = new GraphQlWindow(_api, _permissionService);
            graphQlWindow.Show();
        }

        private void OpenSOAPWindowButton_Click(object sender, RoutedEventArgs e)
        {
            var soapWindow = new SOAPWindow();
            soapWindow.Show();
        }

        private void OpenXRCPWindowButton_Click(object sender, RoutedEventArgs e)
        {
            var cityWindow = new CityWindow();
            cityWindow.Show();
        }
    }
}
