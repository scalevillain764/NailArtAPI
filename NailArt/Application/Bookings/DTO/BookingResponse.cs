using Booking = Domain.Booking;
namespace Application.Bookings.DTO
{
    public record BookingResponse(
        Ulid BookingId,
        string ServiceName,
        string? ServiceShortDescription,
        int DurationHours,
        int DurationMinutes,
        decimal Price,
        string UserName,
        string UserPhone,
        DateTime Date
        )
    {
        public BookingResponse(Booking booking) :
            this(booking.Id,
            booking.Service?.Name,
            booking.Service?.ShortDescription,
            booking.Service!.DurationHours,
            booking.Service!.DurationMinutes,
            booking.Service.Price,
            booking.User?.Name,
            booking.User?.Phone,
            booking.Date)
        { }
    }
}