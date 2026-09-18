using Application.Bookings.DTO;
using Infrastructure.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace Application.Bookings
{
    public record GetBookingsToUserQuery(long UserId, // only for users!!
        int TotalCount, 
        int Page = 1, 
        int PageSize = 1) : IRequest<Result<PagedResponse<BookingResponse>>>;
}