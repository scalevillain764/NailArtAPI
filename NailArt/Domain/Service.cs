namespace Domain
{
    public class Service
    {
        public Ulid Id { get; private set; }
        public string Name { get; set; }
        public string? ShortDescription { get; set; }
        public int DurationHours { get; set; }
        public int DurationMinutes { get; set; }
        public decimal Price { get; set; }
        public Service(string name, string? shortDescription, int durationHours, int durationMinutes, decimal price)
        {
            Id = Ulid.NewUlid();
            Name = name;
            ShortDescription = shortDescription;
            DurationHours = durationHours;
            DurationMinutes = durationMinutes;
            Price = price;
        }
    }
}