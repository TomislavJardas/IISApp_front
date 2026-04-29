using IISApp.Models;
using IISApp.Services;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace IISApp;

public partial class MainWindow : Window
{
    private readonly AppConfig _config = AppConfig.Load();
    private readonly ApiService _api;
    private readonly SoapService _soap;
    private readonly WeatherServiceClient _weather;
    private readonly PermissionService _permission;

    public MainWindow()
    {
        InitializeComponent();
        _api = new ApiService(_config.ApiBaseUrl);
        _soap = new SoapService(_config.SoapBaseUrl.Replace("/ws", ""));
        _weather = new WeatherServiceClient(_config.WeatherServiceUrl);
        _permission = new PermissionService(_config.AccessMode);
        ConfigText.Text = $"API={_config.ApiBaseUrl} | SOAP={_config.SoapBaseUrl} | Weather={_config.WeatherServiceUrl} | RelaxNG={_config.RelaxNgBaseUrl} | AccessMode={_permission.Mode}";
        UpdateRoleState();
    }

    private void UpdateRoleState()
    {
        var canWrite = _permission.CanWrite;
        CreateButton.IsEnabled = canWrite; PatchButton.IsEnabled = canWrite; DeleteButton.IsEnabled = canWrite; XmlSaveButton.IsEnabled = canWrite;
        AuthStatusText.Text = _api.IsAuthenticated ? "Authenticated" : "Not authenticated";
    }

    private async void Login_Click(object sender, RoutedEventArgs e) { AuthStatusText.Text = (await _api.LoginAsync(UsernameText.Text, PasswordText.Password)) ? "Login OK" : "Login failed"; UpdateRoleState(); }
    private async void GetPlayers_Click(object s, RoutedEventArgs e) => RestOutput.Text = JsonSerializer.Serialize(await _api.GetAllPlayersAsync(), new JsonSerializerOptions { WriteIndented = true });
    private async void GetPlayerById_Click(object s, RoutedEventArgs e) => RestOutput.Text = JsonSerializer.Serialize(await _api.GetPlayerByIdAsync(PlayerIdText.Text), new JsonSerializerOptions { WriteIndented = true });
    private async void CreatePlayer_Click(object s, RoutedEventArgs e) { if (!_permission.CanWrite) { MessageBox.Show(_permission.DeniedMessage); return; } RestOutput.Text = JsonSerializer.Serialize(await _api.CreatePlayerAsync(new Player { Name = "Test", Team = "Team", Season = 2025, Points = 11.1 }), new JsonSerializerOptions { WriteIndented = true }); }
    private async void PatchPlayer_Click(object s, RoutedEventArgs e) { if (!_permission.CanWrite) { MessageBox.Show(_permission.DeniedMessage); return; } RestOutput.Text = JsonSerializer.Serialize(await _api.UpdatePlayerAsync(new Player { Id = PlayerIdText.Text, Name = "Updated", Team = "Updated", Season = 2026, Points = 14.2 }), new JsonSerializerOptions { WriteIndented = true }); }
    private async void DeletePlayer_Click(object s, RoutedEventArgs e) { if (!_permission.CanWrite) { MessageBox.Show(_permission.DeniedMessage); return; } RestOutput.Text = (await _api.DeletePlayerAsync(PlayerIdText.Text)).ToString(); }
    private void LoadSampleXml_Click(object s, RoutedEventArgs e) => XmlInput.Text = "<Players><Player><name>Nikola Jokic</name><team>Denver Nuggets</team><season>2025</season><points>26.4</points></Player></Players>";
    private async void SaveXml_Click(object s, RoutedEventArgs e)
    {
        if (!_permission.CanWrite) { MessageBox.Show(_permission.DeniedMessage); return; }
        var r = await _api.HttpClient.PostAsync("/validateAndSaveXml", new StringContent(XmlInput.Text, Encoding.UTF8, "application/xml"));
        XmlOutput.Text = $"{(int)r.StatusCode} {r.StatusCode}\n{await r.Content.ReadAsStringAsync()}";
    }
    private async void SoapSearch_Click(object s, RoutedEventArgs e) { try { SoapOutput.Text = JsonSerializer.Serialize(await _soap.SearchPlayersAsync(SoapSearchText.Text), new JsonSerializerOptions { WriteIndented = true }); } catch (Exception ex) { SoapOutput.Text = ex.ToString(); } }
    private async void WeatherLookup_Click(object s, RoutedEventArgs e) { try { WeatherOutput.Text = JsonSerializer.Serialize(await _weather.GetTemperaturesAsync(CityText.Text), new JsonSerializerOptions { WriteIndented = true }); } catch (Exception ex) { WeatherOutput.Text = ex.ToString(); } }
    private async void HealthChecks_Click(object s, RoutedEventArgs e)
    {
        var sb = new StringBuilder();
        await Check(sb, "API /actuator?", () => _api.HttpClient.GetAsync("/api/players"));
        await Check(sb, "SOAP WSDL", () => new HttpClient().GetAsync(_config.SoapBaseUrl + "/players.wsdl"));
        await Check(sb, "Weather", () => new HttpClient().PostAsync(_config.WeatherServiceUrl, new StringContent("<methodCall><methodName>WeatherService.getTemperature</methodName><params><param><value><string>Lon</string></value></param></params></methodCall>", Encoding.UTF8, "text/xml")));
        await Check(sb, "RelaxNG /validate", () => new HttpClient().PostAsync(_config.RelaxNgBaseUrl + "/validate", new StringContent("<Players></Players>", Encoding.UTF8, "application/xml")));
        HealthOutput.Text = sb.ToString();
    }
    private static async System.Threading.Tasks.Task Check(StringBuilder sb, string name, Func<System.Threading.Tasks.Task<HttpResponseMessage>> action) { try { var r = await action(); sb.AppendLine($"{name}: {(int)r.StatusCode} {r.StatusCode}"); } catch (Exception ex) { sb.AppendLine($"{name}: FAIL {ex.Message}"); } }
}
