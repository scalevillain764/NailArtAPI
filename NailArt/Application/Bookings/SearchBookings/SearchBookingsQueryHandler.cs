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
    public class SearchBookingsQueryHandler : IRequestHandler<SearchBookingsQuery, Result<PagedResponse<AdminBookingCardResponse>>>
    {
        private readonly AppDbContext _context;
        public SearchBookingsQueryHandler(AppDbContext context)
        {
            _context = context;
        }
        public async Task<Result<PagedResponse<AdminBookingCardResponse>>> Handle(SearchBookingsQuery query, CancellationToken token)
        {
            var q = _context.bookings
                .Include(x => x.Service)
                .AsQueryable();

            if (query.Id != null)
                q = q.Where(x => x.Id == query.Id);

            if (query.ServiceName != null)
            {
                var words = query.ServiceName
                    .ToLower()
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);

                foreach(var word in words)
                {
                    q = q.Where(x => x.Service.Name
                    .ToLower()
                    .Contains(word));
                }
            }

            if(query.Date != null)
                q = q.Where(x => x.Date.Date == query.Date.Value.Date);

            if(query.Status != null)
            {
                if(Enum.TryParse<Status>(query.Status, out var stat))
                {
                    q = q.Where(x => x.Status == stat);
                }
            }

            int totalCount = await q.CountAsync(token);

            var rez = await q
                .Select(x => new AdminBookingCardResponse(x))
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(token);

            return Result<PagedResponse<AdminBookingCardResponse>>.Success(
                new PagedResponse<AdminBookingCardResponse>(rez, query.Page, query.PageSize, totalCount));
        }
    }
}