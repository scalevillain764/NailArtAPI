using Booking = Domain.Booking;
namespace Application.Bookings.DTO
{
    public record BookingResponse(
        Ulid BookingId,
        string ServiceName,
        string? ServiceShortDescription,
        int DurationMinutes,
        int DurationHours,
        int DurationMinutesFromHours,
        decimal Price,
        string UserName,
        string UserPhone,
        DateTime Date,
        string Status
        )
    {
        public BookingResponse(Booking booking) :
            this(booking.Id,
            booking.Service?.Name,
            booking.Service?.ShortDescription,
            booking.Service.DurationMinutes,
            booking.Service.DurationMinutes / 60,
            booking.Service.DurationMinutes - (60 * (booking.Service.DurationMinutes / 60)),
            booking.Service.Price,
            booking.User?.Name,
            booking.User?.Phone,
            booking.Date,
            booking.Status.ToString())
        { }
    }
}