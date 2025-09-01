using System;
using System.Text;
using System.Windows;
using IISApp.Models;
using IISApp.Services;

namespace IISApp
{
    public partial class PlayersWindow : Window
    {
        private readonly ApiService _api;
        private readonly ValidationService _validator;

        public PlayersWindow(ApiService api, ValidationService validator)
        {
            InitializeComponent();
            _api = api;
            _validator = validator;
        }

        private Player BuildPlayer(bool includeId = true)
        {
            return new Player
            {
                Id = int.TryParse(IdTextBox.Text, out var id) ? id : 0,
                Name = NameTextBox.Text,
                Team = TeamTextBox.Text,
                Season = SeasonTextBox.Text,
                Points = double.TryParse(PointsTextBox.Text, out var p) ? p : 0
            };
        }

        private string BuildPlayerXml(Player player)
        {
            var sb = new StringBuilder();
            sb.Append("<Players><Player>");
            sb.Append($"<name>{player.Name}</name>");
            sb.Append($"<team>{player.Team}</team>");
            sb.Append($"<season>{player.Season}</season>");
            sb.Append($"<points>{player.Points}</points>");
            sb.Append("</Player></Players>");
            return sb.ToString();
        }

        private string GetSelectedSchema()
        {
            if (SchemaComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem item)
                return item.Content?.ToString() ?? "xsd";
            return "xsd";
        }

        private async void LoadAllButton_Click(object sender, RoutedEventArgs e)
        {
            var players = await _api.GetAllPlayersAsync();
            PlayersListBox.ItemsSource = players;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var player = BuildPlayer();
            var xml = BuildPlayerXml(player);
            var schema = GetSelectedSchema();
            var validation = await _validator.ValidateAsync(xml, schema);
            if (!string.IsNullOrWhiteSpace(validation))
            {
                MessageBox.Show(validation, "Validation result");
            }

            bool success;
            if (player.Id > 0)
                success = await _api.UpdatePlayerAsync(player);
            else
                success = await _api.CreatePlayerAsync(player);

            MessageBox.Show(success ? "Saved" : "Save failed");
            LoadAllButton_Click(sender, e);
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(IdTextBox.Text, out var id) && id > 0)
            {
                var success = await _api.DeletePlayerAsync(id);
                MessageBox.Show(success ? "Deleted" : "Delete failed");
                LoadAllButton_Click(sender, e);
            }
        }

        private async void ValidateButton_Click(object sender, RoutedEventArgs e)
        {
            var player = BuildPlayer();
            var xml = BuildPlayerXml(player);
            var schema = GetSelectedSchema();
            var result = await _validator.ValidateAsync(xml, schema);
            MessageBox.Show(result, "Validation result");
        }
    }
}
