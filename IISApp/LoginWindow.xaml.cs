using System;
using System.Windows;
using IISApp.Services;

namespace IISApp
{
    public partial class LoginWindow : Window
    {
        private readonly ApiService _api;
        private readonly ValidationService _validator;
        private readonly PermissionService _permissionService;

        public LoginWindow(ApiService api, ValidationService validator, PermissionService permissionService)
        {
            InitializeComponent();
            _api = api;
            _validator = validator;
            _permissionService = permissionService;
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var username = UsernameTextBox.Text.Trim();
            var password = PasswordBox.Password.Trim();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                LoginStatusTextBlock.Text = "Enter username and password.";
                return;
            }

            LoginButton.IsEnabled = false;
            try
            {
                var loginSuccess = await _api.LoginAsync(username, password);
                if (!loginSuccess)
                {
                    LoginStatusTextBlock.Text = "Login failed. Check credentials and backend status.";
                    return;
                }

                LoginStatusTextBlock.Text = "Login successful.";
                var playersWindow = new PlayersWindow(_api, _validator, _permissionService);
                playersWindow.Show();
                Close();
            }
            catch (Exception ex)
            {
                LoginStatusTextBlock.Text = $"Login failed: {ex.Message}";
            }
            finally
            {
                LoginButton.IsEnabled = true;
            }
        }
    }
}
