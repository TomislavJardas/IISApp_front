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

        public ValidateAndSaveWindow() : this(new ApiService(AppConfig.ApiBaseUrl))
        {
        }

        public ValidateAndSaveWindow(ApiService api)
        {
            InitializeComponent();
            _validator = new ValidationService(api);
        }

        private string BuildPlayerXml()
        {
            var xml = new XElement("Players",
                new XElement("Player",
                    new XElement("name", NameTextBox.Text.Trim()),
                    new XElement("team", TeamTextBox.Text.Trim()),
                    new XElement("season", int.Parse(SeasonTextBox.Text.Trim(), CultureInfo.InvariantCulture)),
                    new XElement("points", double.Parse(PointsTextBox.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture))));

            return xml.ToString(SaveOptions.DisableFormatting);
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
            // Backend currently exposes only validate+save (/validateAndSaveXml).
            await ExecuteValidateAndSaveAsync("Validate (backend validates and saves)");
        }

        private async void ValidateAndSaveButton_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteValidateAndSaveAsync("Validate & Save");
        }

        private async System.Threading.Tasks.Task ExecuteValidateAndSaveAsync(string actionLabel)
        {
            if (!TryValidateInputs(out var validationError))
            {
                ResponseTextBox.Text = validationError;
                return;
            }

            var xml = BuildPlayerXml();
            var schema = GetSchema();

            try
            {
                var result = await _validator.ValidateAndSaveAsync(xml, schema);
                ResponseTextBox.Text = $"{actionLabel}:{System.Environment.NewLine}{result}";
            }
            catch (System.Exception ex)
            {
                ResponseTextBox.Text = $"Error: {ex.Message}";
            }
        }

        private bool TryValidateInputs(out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text) || string.IsNullOrWhiteSpace(TeamTextBox.Text))
            {
                errorMessage = "User invalid: name and team are required.";
                return false;
            }

            if (!int.TryParse(SeasonTextBox.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
            {
                errorMessage = "User invalid: season must be a valid integer.";
                return false;
            }

            if (!double.TryParse(PointsTextBox.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            {
                errorMessage = "User invalid: points must be a valid number.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }
    }
}
