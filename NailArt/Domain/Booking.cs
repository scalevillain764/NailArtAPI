using BStatus = Domain.Enums.BookingStatus;
namespace Domain
{
    public class Booking
    {
        public Ulid Id { get; init; }

        public long UserId { get; init; }
        public User? User { get; set; } = null!;

        public Ulid ServiceId { get; init; }
        public Service? Service { get; set; } = null!;

        public DateTime Date { get; init; }
        public BStatus Status { get; set; }

        public Booking(long userId, Ulid serviceId, DateTime date)
        {
            Id = Ulid.NewUlid();
            UserId = userId;
            ServiceId = serviceId;
            Date = date;
            Status = BStatus.Pending;
        }
    }
}