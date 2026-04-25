using System.Globalization;
using System.Text.Json.Serialization;

namespace IISApp.Models
{
    public class Player
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("team")]
        public string? Team { get; set; }

        [JsonPropertyName("season")]
        public int Season { get; set; }

        [JsonPropertyName("points")]
        public double Points { get; set; }

        public override string ToString()
        {
            var idText = string.IsNullOrWhiteSpace(Id) ? "(new)" : Id;
            return $"[{idText}] {Name} | {Team} | Season {Season} | Points {Points.ToString(CultureInfo.InvariantCulture)}";
        }
    }
}
