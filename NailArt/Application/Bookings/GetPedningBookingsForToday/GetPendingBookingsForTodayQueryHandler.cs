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
    public class GetPendingBookingsForTodayQueryHandler : IRequestHandler<GetPendingBookingsForTodayQuery, Result<PagedResponse<AdminBookingCardResponse>>>
    {
        private readonly AppDbContext _context;
        public GetPendingBookingsForTodayQueryHandler(AppDbContext context)
        {
            _context = context;
        }
        public async Task<Result<PagedResponse<AdminBookingCardResponse>>> Handle(GetPendingBookingsForTodayQuery query, CancellationToken token)
        {
            if (query.Page < 1 || query.PageSize < 1)
                return Result<PagedResponse<AdminBookingCardResponse>>.Fail("Проверьте параметры страницы", Error.Validation);

            var q = _context.bookings
                .Include(x => x.User)
                .Include(x => x.Service)
                .Where(x => x.Date.Date == DateTime.UtcNow.Date);

            int totalCount = await q.CountAsync(token);

            var rez = await q
                .Select(x => new AdminBookingCardResponse(x))
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(token);

            return Result<PagedResponse<AdminBookingCardResponse>>.Success(new PagedResponse<AdminBookingCardResponse>(rez, query.Page, query.PageSize, totalCount));
        }
    }
}