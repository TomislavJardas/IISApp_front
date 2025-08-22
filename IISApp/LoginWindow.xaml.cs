using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public string JwtToken { get; private set; }

        public LoginWindow()
        {
            InitializeComponent();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password.Trim();

            string jsonBody = $"{{\"username\":\"{username}\",\"password\":\"{password}\"}}";

            try
            {
                using HttpClient client = new HttpClient();
                StringContent content = new StringContent(jsonBody, new UTF8Encoding(false), "application/json");

                HttpResponseMessage response = await client.PostAsync("http://localhost:8080/api/auth/login", content);
                response.EnsureSuccessStatusCode();


                string responseContent = await response.Content.ReadAsStringAsync();
                JwtToken = responseContent.Trim();

                if (!string.IsNullOrEmpty(JwtToken))
                {
                    LoginStatusTextBlock.Text = "Login successful!";
                    CountriesWindow countriesWindow = new CountriesWindow(JwtToken);
                    countriesWindow.Show();
                    this.Close();
                }
                else
                {
                    LoginStatusTextBlock.Text = "Login failed: No token received.";
                }
            }
            catch (Exception ex)
            {
                LoginStatusTextBlock.Text = $"Login failed: {ex.Message}";
            }
        }
    }


}

