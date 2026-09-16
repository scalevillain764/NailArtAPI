using MediatR;
using Infrastructure.Responses;
namespace Application.Bookings
{
    public record CancelBookingCommand(long userId, Ulid bookingId) : IRequest<Result<string>>;
}