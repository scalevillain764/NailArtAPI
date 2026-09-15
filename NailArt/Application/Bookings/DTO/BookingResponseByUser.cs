using Booking = Domain.Booking;
namespace Application.Bookings.DTO
{
    public record BookingResponseByUser(
        Ulid BookingId, 
        string ServiceName, 
        string? ServiceShortDescription,
        decimal Price,
        DateTime Date,
        string Status
        )
    {
        public BookingResponseByUser(Booking booking) : this(
            booking.Id, 
            booking.Service.Name,
            booking.Service.ShortDescription,
            booking.Service.Price,
            booking)
    }
}