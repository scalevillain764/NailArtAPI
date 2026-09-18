using Booking = Domain.Booking;
namespace Application.Bookings.DTO
{
    public record AdminBookingCardResponse(
        Ulid BookingId,
        string ServiceName,
        decimal Price,
        string Name,
        string UserPhone,
        DateTime Date
        )
    {
        public AdminBookingCardResponse(Booking booking) :
            this(booking.Id,
            booking.Service?.Name,
            booking.Service.Price,
            booking.User?.Name,
            booking.User?.Phone,
            booking.Date)
        { }
    }
}