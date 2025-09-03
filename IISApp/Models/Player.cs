namespace IISApp.Models
{
    public class Player
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Team { get; set; }
        public string? Season { get; set; }
        public double Points { get; set; }

        public override string? ToString()
        {
            return $"Id: {Id}, Name: {Name}, Team: {Team}, Season: {Season}, Points: {Points}";
        }
    }
}
