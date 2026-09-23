namespace TelegramBot.DTO.Bookings
{ 
    public class CreateBookingDraft
    {
        public long? UserId { get; set; }
        public Ulid? ServiceId { get; set; }
        public DateTime? DateTime { get; set; }
        public CreateBookingDraft(long? userId, Ulid? serviceId, DateTime? dateTime)
        {
            UserId = userId;
            ServiceId = serviceId;
            DateTime = dateTime;
        }
    }
}
