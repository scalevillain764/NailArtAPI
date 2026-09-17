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
    public class GetPendingBookingsForTodayQueryHandler : IRequestHandler<GetPendingBookingsForTodayQuery, Result<PagedResponse<BookingResponse>>>
    {
        private readonly AppDbContext _context;
        public GetPendingBookingsForTodayQueryHandler(AppDbContext context)
        {
            _context = context;
        }
        public async Task<Result<PagedResponse<BookingResponse>>> Handle(GetPendingBookingsForTodayQuery query, CancellationToken token)
        {
            if (query.Page < 1 || query.PageSize < 1)
                return Result<PagedResponse<BookingResponse>>.Fail("Проверьте параметры страницы", Error.Validation);

            var q = _context.bookings
                .Where(x => x.Date.Date == DateTime.UtcNow.Date);

            int totalCount = await q.CountAsync(token);

            var rez = await q
                .Select(x => new BookingResponse(x))
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(token);

            return Result<PagedResponse<BookingResponse>>.Success(new PagedResponse<BookingResponse>(rez, query.Page, query.PageSize, totalCount));
        }
    }
}