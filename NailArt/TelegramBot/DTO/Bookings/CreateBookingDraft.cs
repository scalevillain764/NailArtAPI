namespace TelegramBot.DTO.Bookings
{ 
    public class CreateBookingDraft
    {
        public long? UserId { get; set; }
        public Ulid? ServiceId { get; set; }
        public TimeOnly? Time { get; set; }
        public DateOnly? Date { get; set; }
        public DateTime? DateTime { get; set; }
        public CreateBookingDraft(long? userId, Ulid? serviceId, DateOnly? date, TimeOnly? time, DateTime? dateTime)
        {
            UserId = userId;
            ServiceId = serviceId;
            Date = date;
            Time = time;
            DateTime = dateTime;
        }
    }
}
