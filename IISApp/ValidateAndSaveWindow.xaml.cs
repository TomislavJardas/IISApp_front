using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using IISApp.Services;

namespace IISApp
{
    public partial class ValidateAndSaveWindow : Window
    {
        private readonly ValidationService _validator;

        public ValidateAndSaveWindow() : this(new ApiService(AppConfig.Load().ApiBaseUrl))
        {
        }

        public ValidateAndSaveWindow(ApiService api)
        {
            InitializeComponent();
            _validator = new ValidationService(api);
        }

        private bool TryBuildPlayerXml(out string xml, out string errorMessage)
        {
            var errors = new List<string>();
            xml = string.Empty;

            var name = NameTextBox.Text.Trim();
            var team = TeamTextBox.Text.Trim();
            var seasonText = SeasonTextBox.Text.Trim();
            var pointsText = PointsTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(name)) errors.Add("Name is required.");
            if (string.IsNullOrWhiteSpace(team)) errors.Add("Team is required.");

            int season = 0;
            if (string.IsNullOrWhiteSpace(seasonText)) errors.Add("Season is required.");
            else if (!int.TryParse(seasonText, out season)) errors.Add("Season must be a valid whole number.");
            else if (season <= 0) errors.Add("Season must be greater than 0.");

            double points = 0;
            if (string.IsNullOrWhiteSpace(pointsText)) errors.Add("Points are required.");
            else if (!double.TryParse(pointsText, NumberStyles.Float, CultureInfo.InvariantCulture, out points)) errors.Add("Points must be a valid decimal number.");
            else if (points < 0) errors.Add("Points must be greater than or equal to 0.");

            errorMessage = string.Join(Environment.NewLine, errors);
            if (errors.Count > 0) return false;

            var parsedXml = new XElement("Players",
                new XElement("Player",
                    new XElement("name", name),
                    new XElement("team", team),
                    new XElement("season", season),
                    new XElement("points", points.ToString(CultureInfo.InvariantCulture))));

            xml = parsedXml.ToString(SaveOptions.DisableFormatting);
            return true;
        }

        private string GetSchema()
        {
            if (SchemaComboBox.SelectedItem is ComboBoxItem item)
            {
                return item.Content?.ToString()?.ToLowerInvariant() ?? "xsd";
            }

            return "xsd";
        }

        private async void ValidateButton_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteValidateAndSaveAsync("Validate (backend validates and saves)");
        }

        private async void ValidateAndSaveButton_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteValidateAndSaveAsync("Validate & Save");
        }

        private async System.Threading.Tasks.Task ExecuteValidateAndSaveAsync(string actionLabel)
        {
            if (!TryBuildPlayerXml(out var xml, out var validationErrors))
            {
                ResponseTextBox.Text = $"Validation failed:{Environment.NewLine}- {validationErrors.Replace(Environment.NewLine, Environment.NewLine + "- ")}";
                return;
            }

            var schema = GetSchema();

            try
            {
                var result = await _validator.ValidateAndSaveAsync(xml, schema);
                ResponseTextBox.Text = $"{actionLabel}:{Environment.NewLine}{result}";
            }
            catch (Exception ex)
            {
                ResponseTextBox.Text = $"Error: {ex.Message}";
            }
        }
    }
}
