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
    public class GetBookingsToUserQueryHandler : IRequestHandler<GetBookingsToUserQuery, Result<PagedResponse<BookingResponse>>>
    {
        private readonly AppDbContext _context;
        public GetBookingsToUserQueryHandler(AppDbContext context)
        {
            _context = context;
        }
        public async Task<Result<PagedResponse<BookingResponse>>> Handle(GetBookingsToUserQuery query, CancellationToken token)
        {
            if (query.Page < 1 || query.PageSize < 1)
                return Result<PagedResponse<BookingResponse>>.Fail("Ошибка страницы", Error.Validation);

            var q = _context.bookings
                .Where(x => x.UserId == query.UserId)
                .Include(x => x.Service)
                .Include(x => x.User);

            int totalCount = await q.CountAsync(token);

            var rez = await _context.bookings
                .Select(x => new BookingResponse(x))
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(token);

            return Result<PagedResponse<BookingResponse>>
                .Success(new PagedResponse<BookingResponse>(rez, query.Page, query.PageSize, totalCount));
        }
    }
}