using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using IISApp.Models;
using IISApp.Services;

namespace IISApp
{
    public partial class PlayersWindow : Window
    {
        private readonly ApiService _api;
        private readonly ValidationService _validator;
        private readonly PermissionService _permissions;

        public PlayersWindow(ApiService api, ValidationService validator, PermissionService permissions)
        {
            InitializeComponent();
            _api = api;
            _validator = validator;
            _permissions = permissions;

            _api.SessionExpired += OnSessionExpired;
            ConfigurePermissions();
            Loaded += (_, _) => _ = LoadPlayersAsync();
        }

        private void ConfigurePermissions()
        {
            var isReadOnly = !_permissions.CanMutatePlayers;
            SaveButton.IsEnabled = !isReadOnly;
            DeleteButton.IsEnabled = !isReadOnly;
            AccessModeTextBlock.Text = isReadOnly ? "Mode: Read-only" : "Mode: Full-access";
        }

        private void PopulateForm(Player player)
        {
            IdTextBox.Text = player.Id ?? string.Empty;
            NameTextBox.Text = player.Name ?? string.Empty;
            TeamTextBox.Text = player.Team ?? string.Empty;
            SeasonTextBox.Text = player.Season.ToString();
            PointsTextBox.Text = player.Points.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private void ClearForm()
        {
            IdTextBox.Text = string.Empty;
            NameTextBox.Text = string.Empty;
            TeamTextBox.Text = string.Empty;
            SeasonTextBox.Text = string.Empty;
            PointsTextBox.Text = string.Empty;
            PlayersListBox.SelectedItem = null;
        }

        private string BuildPlayerXml(string name, string team, string seasonText, string pointsText)
        {
            var xml = new XElement("Players",
                new XElement("Player",
                    new XElement("name", name),
                    new XElement("team", team),
                    new XElement("season", seasonText),
                    new XElement("points", pointsText)));

            return xml.ToString(SaveOptions.DisableFormatting);
        }

        private string GetSelectedSchema()
        {
            if (SchemaComboBox.SelectedItem is ComboBoxItem item)
            {
                return item.Content?.ToString()?.ToLowerInvariant() ?? "xsd";
            }

            return "xsd";
        }

        private async System.Threading.Tasks.Task LoadPlayersAsync()
        {
            if (!_api.IsAuthenticated)
            {
                StatusTextBlock.Text = "Please login to load players.";
                return;
            }

            var players = await _api.GetAllPlayersAsync();
            PlayersListBox.ItemsSource = players;
            StatusTextBlock.Text = $"Loaded {players?.Length ?? 0} players.";
        }

        private async void LoadAllButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadPlayersAsync();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_permissions.CanMutatePlayers)
            {
                MessageBox.Show("Read-only mode is enabled. Save is disabled.");
                return;
            }

            if (!_api.IsAuthenticated)
            {
                MessageBox.Show("Please login first.");
                return;
            }

            var id = IdTextBox.Text.Trim();
            var name = NameTextBox.Text.Trim();
            var team = TeamTextBox.Text.Trim();
            var seasonText = SeasonTextBox.Text.Trim();
            var pointsText = PointsTextBox.Text.Trim();

            if (!TryValidatePlayerInputs(name, team, seasonText, pointsText, out var validationError))
            {
                MessageBox.Show(validationError, "Player validation failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                ApiResult<Player> result;
                if (string.IsNullOrWhiteSpace(id))
                {
                    result = await _api.CreatePlayerFromRawAsync(name, team, seasonText, pointsText);
                    if (!result.Success)
                    {
                        MessageBox.Show(result.ErrorMessage, "Player validation failed", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    MessageBox.Show("Player created.");
                }
                else
                {
                    result = await _api.UpdatePlayerFromRawAsync(id, name, team, seasonText, pointsText);
                    if (!result.Success)
                    {
                        MessageBox.Show(result.ErrorMessage, "Player validation failed", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    MessageBox.Show("Player updated.");
                }

                await LoadPlayersAsync();
                if (result.Data is not null)
                {
                    PopulateForm(result.Data);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Save failed: {ex.Message}");
            }
        }

        private bool TryValidatePlayerInputs(
            string name,
            string team,
            string seasonText,
            string pointsText,
            out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                errorMessage = "Name is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(team))
            {
                errorMessage = "Team is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(seasonText))
            {
                errorMessage = "Season is required.";
                return false;
            }

            if (!int.TryParse(seasonText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var season))
            {
                errorMessage = "Season must be a valid whole number.";
                return false;
            }

            if (season <= 0)
            {
                errorMessage = "Season must be greater than 0.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(pointsText))
            {
                errorMessage = "Points is required.";
                return false;
            }

            if (!double.TryParse(pointsText, NumberStyles.Float, CultureInfo.InvariantCulture, out var points))
            {
                errorMessage = "Points must be a valid number.";
                return false;
            }

            if (points < 0)
            {
                errorMessage = "Points must be greater than or equal to 0.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_permissions.CanMutatePlayers)
            {
                MessageBox.Show("Read-only mode is enabled. Delete is disabled.");
                return;
            }

            var recordId = IdTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(recordId))
            {
                MessageBox.Show("Select a player first.");
                return;
            }

            var confirm = MessageBox.Show($"Delete player {recordId}?", "Confirm delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            var success = await _api.DeletePlayerAsync(recordId);
            MessageBox.Show(success ? "Player deleted." : "Delete failed.");

            if (success)
            {
                ClearForm();
                await LoadPlayersAsync();
            }
        }

        private async void ValidateButton_Click(object sender, RoutedEventArgs e)
        {
            var name = NameTextBox.Text.Trim();
            var team = TeamTextBox.Text.Trim();
            var seasonText = SeasonTextBox.Text.Trim();
            var pointsText = PointsTextBox.Text.Trim();
            var xml = BuildPlayerXml(name, team, seasonText, pointsText);
            var schema = GetSelectedSchema();
            var result = await _validator.ValidateAndSaveAsync(xml, schema);
            MessageBox.Show(result, "Validate & Save result");
        }

        private async void PlayersListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PlayersListBox.SelectedItem is not Player selectedPlayer)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(selectedPlayer.Id))
            {
                PopulateForm(selectedPlayer);
                return;
            }

            var detailed = await _api.GetPlayerByIdAsync(selectedPlayer.Id);
            PopulateForm(detailed ?? selectedPlayer);
        }

        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            StatusTextBlock.Text = "Creating a new player.";
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            _api.Logout();
            MessageBox.Show("Logged out.");
            var loginWindow = new LoginWindow(_api, _validator, _permissions);
            loginWindow.Show();
            Close();
        }

        private void OnSessionExpired()
        {
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show("Session expired. Please login again.");
                var loginWindow = new LoginWindow(_api, _validator, _permissions);
                loginWindow.Show();
                Close();
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            _api.SessionExpired -= OnSessionExpired;
            base.OnClosed(e);
        }
    }
}
