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
    public class GetBookingsByUserQueryHandler : IRequestHandler<GetBookingsByUserQuery, Result<PagedResponse<BookingResponseByUser>>>
    {
        private readonly AppDbContext _context;
        public GetBookingsByUserQueryHandler(AppDbContext context)
        {
            _context = context;
        }
        public async Task<Result<PagedResponse<BookingResponseByUser>>> Handle(GetBookingsByUserQuery query, CancellationToken token)
        {
            if (query.Page < 1 || query.PageSize < 1)
                return Result<PagedResponse<BookingResponseByUser>>.Fail("Ошибка страницы", Error.Validation);

            var rez = await _context.bookings
                .Where(x => x.UserId == query.UserId)
                .ToListAsync(token);

            int totalCount = rez.Count();

            return Result<PagedResponse<BookingResponseByUser>>.Success(new PagedResponse(rez, query.Page, query.PageSize, totalCount));
        }
    }
}