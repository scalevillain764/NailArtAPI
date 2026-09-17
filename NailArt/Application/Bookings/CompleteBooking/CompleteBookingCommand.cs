using MediatR;
using Infrastructure.Responses;
namespace Application.Bookings
{
    public record CompleteBookingCommand(Ulid bookingId) : IRequest<Result<string>>;
}