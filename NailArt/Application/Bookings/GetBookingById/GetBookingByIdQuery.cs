using Application.Bookings.DTO;
using Infrastructure.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace Application.Bookings
{
    public record GetBookingByIdQuery(Ulid Id) : IRequest<Result<BookingResponse>>;
}