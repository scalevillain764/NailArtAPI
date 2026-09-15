using MediatR;
using Infrastructure.Responses;
using Application.Bookings.DTO;
namespace Application.Bookings
{
    public record CreateBookingCommand(long UserId, Ulid ServiceId, DateTime Date) : IRequest<Result<BookingResponse>>;
}