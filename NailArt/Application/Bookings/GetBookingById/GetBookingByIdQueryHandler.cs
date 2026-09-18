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
    public class GetBookingByIdQueryHandler : IRequestHandler<GetBookingByIdQuery, Result<BookingResponse>>
    {
        private readonly AppDbContext _context;
        public GetBookingByIdQueryHandler(AppDbContext context)
        {
            _context = context;
        }
        public async Task<Result<BookingResponse>> Handle(GetBookingByIdQuery query, CancellationToken token)
        {
            var rez = await _context.bookings
                .Include(x => x.User)
                .Include(x => x.Service)
                .FirstOrDefaultAsync(x => x.Id == query.Id, token);

            return rez != null ? Result<BookingResponse>.Success(new BookingResponse(rez))
                : Result<BookingResponse>.Fail("Запись не найдена", Error.NotFound);

        }
    }
}