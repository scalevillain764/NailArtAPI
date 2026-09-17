using MediatR;
using Infrastructure.Responses;
using Application.Bookings.DTO;
namespace Application.Bookings
{
    public record GetPendingBookingsForTodayQuery(int Page, int PageSize, int TotalCount) : IRequest<Result<PagedResponse<BookingResponse>>>;
}