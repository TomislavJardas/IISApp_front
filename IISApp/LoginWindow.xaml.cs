using System;
using System.Windows;
using IISApp.Services;

namespace IISApp
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private readonly ApiService _api;
        private readonly ValidationService _validator;

        public LoginWindow(ApiService api, ValidationService validator)
        {
            InitializeComponent();
            _api = api;
            _validator = validator;
        }
        // Add this property to the LoginWindow class to fix CS0103
        public string AccessToken { get; private set; }
        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password.Trim();

            try
            {
                // ApiService.LoginAsync returns a bool, not a token string
                bool loginSuccess = await _api.LoginAsync(username, password);
                if (loginSuccess)
                {
                    AccessToken = _api.AccessToken?.Trim(); // Get token from ApiService property
                    LoginStatusTextBlock.Text = "Login successful!";
                    PlayersWindow playersWindow = new PlayersWindow(_api, _validator);
                    playersWindow.Show();
                    this.Close();
                }
                else
                {
                    LoginStatusTextBlock.Text = "Login failed.";
                }
            }
            catch (Exception ex)
            {
                LoginStatusTextBlock.Text = $"Login failed: {ex.Message}";
            }
        }
    }
}
