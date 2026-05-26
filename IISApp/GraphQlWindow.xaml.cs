using System;
using System.Globalization;
using System.Windows;
using IISApp.Models;
using IISApp.Services;

namespace IISApp
{
    public partial class GraphQlWindow : Window
    {
        private readonly ApiService _api;
        private readonly GraphQlService _graphQl;
        private readonly PermissionService _permissions;

        public GraphQlWindow(ApiService api, PermissionService permissions)
        {
            InitializeComponent();
            _api = api;
            _permissions = permissions;
            _graphQl = new GraphQlService(_api);

            _api.SessionExpired += OnSessionExpired;
            ConfigurePermissions();
            Loaded += async (_, _) => await LoadPlayersAsync();
        }

        private void ConfigurePermissions()
        {
            var isReadOnly = !_permissions.CanMutatePlayers;
            CreateButton.IsEnabled = !isReadOnly;
            UpdateButton.IsEnabled = !isReadOnly;
            DeleteButton.IsEnabled = !isReadOnly;
            AccessModeTextBlock.Text = isReadOnly ? "Mode: Read-only" : "Mode: Full-access";
        }

        private async System.Threading.Tasks.Task LoadPlayersAsync()
        {
            if (!EnsureAuthenticated()) return;
            var result = await _graphQl.GetPlayersAsync();
            if (!result.Success)
            {
                StatusTextBlock.Text = result.ErrorMessage;
                return;
            }

            PlayersDataGrid.ItemsSource = result.Data ?? Array.Empty<Player>();
            StatusTextBlock.Text = $"Loaded {result.Data?.Length ?? 0} players.";
        }

        private async void LoadPlayersButton_Click(object sender, RoutedEventArgs e) => await LoadPlayersAsync();

        private async void LoadByIdButton_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureAuthenticated()) return;
            var id = IdTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(id))
            {
                MessageBox.Show("Enter a record ID first.");
                return;
            }

            var result = await _graphQl.GetPlayerByIdAsync(id);
            if (!result.Success || result.Data is null)
            {
                MessageBox.Show(result.ErrorMessage ?? "Player was not found.");
                return;
            }

            PopulateForm(result.Data);
            StatusTextBlock.Text = "Player loaded.";
        }

        private async void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CanMutate()) return;
            if (!TryGetFormValues(out var id, out var name, out var team, out var season, out var points)) return;

            var result = await _graphQl.CreatePlayerAsync(name, team, season, points);
            if (!result.Success || result.Data is null)
            {
                MessageBox.Show(result.ErrorMessage ?? "Create failed.");
                return;
            }

            PopulateForm(result.Data);
            await LoadPlayersAsync();
            StatusTextBlock.Text = "Player created.";
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CanMutate()) return;
            if (!TryGetFormValues(out var id, out var name, out var team, out var season, out var points)) return;
            if (string.IsNullOrWhiteSpace(id))
            {
                MessageBox.Show("Record ID is required for update.");
                return;
            }

            var result = await _graphQl.UpdatePlayerAsync(id, name, team, season, points);
            if (!result.Success || result.Data is null)
            {
                MessageBox.Show(result.ErrorMessage ?? "Update failed.");
                return;
            }

            PopulateForm(result.Data);
            await LoadPlayersAsync();
            StatusTextBlock.Text = "Player updated.";
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CanMutate()) return;
            if (!EnsureAuthenticated()) return;
            var id = IdTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(id))
            {
                MessageBox.Show("Record ID is required for delete.");
                return;
            }

            var confirm = MessageBox.Show($"Delete player {id}?", "Confirm delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            var result = await _graphQl.DeletePlayerAsync(id);
            if (!result.Success || !result.Data)
            {
                MessageBox.Show(result.ErrorMessage ?? "Delete failed.");
                return;
            }

            ClearForm();
            await LoadPlayersAsync();
            StatusTextBlock.Text = "Player deleted.";
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e) => ClearForm();

        private void PlayersDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (PlayersDataGrid.SelectedItem is Player player)
            {
                PopulateForm(player);
            }
        }

        private void PopulateForm(Player player)
        {
            IdTextBox.Text = player.Id ?? string.Empty;
            NameTextBox.Text = player.Name ?? string.Empty;
            TeamTextBox.Text = player.Team ?? string.Empty;
            SeasonTextBox.Text = player.Season.ToString(CultureInfo.InvariantCulture);
            PointsTextBox.Text = player.Points.ToString(CultureInfo.InvariantCulture);
        }

        private void ClearForm()
        {
            IdTextBox.Text = string.Empty;
            NameTextBox.Text = string.Empty;
            TeamTextBox.Text = string.Empty;
            SeasonTextBox.Text = string.Empty;
            PointsTextBox.Text = string.Empty;
            PlayersDataGrid.SelectedItem = null;
        }

        private bool TryGetFormValues(out string id, out string name, out string team, out int season, out double points)
        {
            id = IdTextBox.Text.Trim();
            name = NameTextBox.Text.Trim();
            team = TeamTextBox.Text.Trim();
            season = 0;
            points = 0;

            if (!EnsureAuthenticated()) return false;
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(team))
            {
                MessageBox.Show("Name and Team are required.");
                return false;
            }

            if (!int.TryParse(SeasonTextBox.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out season))
            {
                MessageBox.Show("Season must be a valid whole number.");
                return false;
            }

            if (!double.TryParse(PointsTextBox.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out points))
            {
                MessageBox.Show("Points must be a valid number.");
                return false;
            }

            return true;
        }

        private bool EnsureAuthenticated()
        {
            if (_api.IsAuthenticated) return true;
            MessageBox.Show("Please login first.");
            return false;
        }

        private bool CanMutate()
        {
            if (_permissions.CanMutatePlayers) return true;
            MessageBox.Show("Read-only mode is enabled.");
            return false;
        }

        private void OnSessionExpired()
        {
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show("Session expired. Please login again.");
                Close();
            });
        }
    }
}
