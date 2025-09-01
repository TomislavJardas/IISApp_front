using System.Windows;
using System.Windows.Controls;
using IISApp.Services;

namespace IISApp
{
    public partial class ValidateAndSaveWindow : Window
    {
        private readonly ValidationService _validator;

        public ValidateAndSaveWindow() : this(new ApiService("http://localhost:8080"))
        {
        }

        public ValidateAndSaveWindow(ApiService api)
        {
            InitializeComponent();
            _validator = new ValidationService(api);
        }

        private string BuildPlayerXml()
        {
            var name = NameTextBox.Text;
            var team = TeamTextBox.Text;
            var season = SeasonTextBox.Text;
            var points = PointsTextBox.Text;

            return $"<Players><Player><name>{name}</name><team>{team}</team><season>{season}</season><points>{points}</points></Player></Players>";
        }

        private string GetSchema()
        {
            if (SchemaComboBox.SelectedItem is ComboBoxItem item)
                return item.Content?.ToString()?.ToLowerInvariant() ?? "xsd";
            return "xsd";
        }

        private async void ValidateButton_Click(object sender, RoutedEventArgs e)
        {
            var xml = BuildPlayerXml();
            var schema = GetSchema();
            try
            {
                var result = await _validator.ValidateAsync(xml, schema);
                ResponseTextBox.Text = result;
            }
            catch (System.Exception ex)
            {
                ResponseTextBox.Text = $"Error: {ex.Message}";
            }
        }

        private async void ValidateAndSaveButton_Click(object sender, RoutedEventArgs e)
        {
            var xml = BuildPlayerXml();
            var schema = GetSchema();
            try
            {
                var result = await _validator.ValidateAndSaveAsync(xml, schema);
                ResponseTextBox.Text = result;
            }
            catch (System.Exception ex)
            {
                ResponseTextBox.Text = $"Error: {ex.Message}";
            }
        }
    }
}

