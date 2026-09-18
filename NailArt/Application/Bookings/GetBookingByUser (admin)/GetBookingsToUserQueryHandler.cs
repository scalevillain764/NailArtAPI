using MediatR;
using Infrastructure.Responses;
using Application.Bookings.DTO;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Error = Domain.Enums.ErrorType;
using Status = Domain.Enums.BookingStatus;
using Domain;
namespace Application.Bookings
{
    public class GetBookingsByUserQueryHandler : IRequestHandler<GetBookingsByUserQuery, Result<PagedResponse<AdminBookingCardResponse>>>
    {
        private readonly AppDbContext _context;
        public GetBookingsByUserQueryHandler(AppDbContext context)
        {
            _context = context;
        }
        public async Task<Result<PagedResponse<AdminBookingCardResponse>>> Handle(GetBookingsByUserQuery query, CancellationToken token)
        {
            if (query.Page < 1 || query.PageSize < 1)
                return Result<PagedResponse<AdminBookingCardResponse>>.Fail("Ошибка страницы", Error.Validation);

            var q = _context.bookings
                .Where(x => x.UserId == query.UserId)
                .Include(x => x.Service)
                .Include(x => x.User);

            int totalCount = await q.CountAsync(token);

            var rez = await _context.bookings
                .Select(x => new AdminBookingCardResponse(x))
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(token);

            return Result<PagedResponse<AdminBookingCardResponse>>
                .Success(new PagedResponse<AdminBookingCardResponse>(rez, query.Page, query.PageSize, totalCount));
        }
    }
}