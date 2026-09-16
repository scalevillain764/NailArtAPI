namespace Domain
{
    public class Service
    {
        public Ulid Id { get; init; }
        public string Name { get; set; }
        public string? ShortDescription { get; set; }
        public int DurationMinutes { get; set; }
        public decimal Price { get; set; }
        public List<Booking> bookings { get; set; } = new();
        public bool IsDeleted { get; set; }
        public Service(string name, string? shortDescription, int durationMinutes, decimal price)
        {
            Id = Ulid.NewUlid();
            Name = name;
            ShortDescription = shortDescription;
            DurationMinutes = durationMinutes;
            Price = price;
            IsDeleted = false;
        }
    }
}