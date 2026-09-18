using MediatR;
using Infrastructure.Responses;
using Application.Bookings.DTO;
namespace Application.Bookings
{
    public record SearchBookingsQuery(
        Ulid? Id,
        string? ServiceName,
        DateTime? Date,
        string? Status,
        int Page, int PageSize, int TotalCount) : IRequest<Result<PagedResponse<AdminBookingCardResponse>>>;
}