namespace TelegramBot.DTO.Bookings
{ 
    public class CreateBookingDraft
    {
        public long? UserId { get; set; }
        public Ulid? ServiceId { get; set; }
        public TimeOnly? Time { get; set; }
        public int? Year { get; set; }
        public int? Month { get; set; }
        public int? Day { get; set; }
        public DateTime? DateTime { get; set; }
        public CreateBookingDraft(long? userId, Ulid? serviceId, int? year, int? month, int? day, TimeOnly? time, DateTime? dateTime)
        {
            UserId = userId;
            ServiceId = serviceId;
            Month = month;
            Year = year;
            Day = day;
            Time = time;
            DateTime = dateTime;
        }
    }
}
