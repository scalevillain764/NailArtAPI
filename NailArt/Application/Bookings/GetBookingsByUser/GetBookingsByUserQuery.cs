using MediatR;
using Infrastructure.Responses;
namespace Application.Bookings
{
    public record GetBookingsByUserQuery(long UserId) : IRequest<>
}