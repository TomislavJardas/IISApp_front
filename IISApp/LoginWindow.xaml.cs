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
                bool success = await _api.LoginAsync(username, password);
                if (success)
                {
                    var token = await response.Content.ReadAsStringAsync();
                    AccessToken = token.Trim(); // Remove whitespace if needed
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
