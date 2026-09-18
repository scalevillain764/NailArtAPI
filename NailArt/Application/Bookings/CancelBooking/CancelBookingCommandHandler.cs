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
   public class CancelBookingCommandHandler : IRequestHandler<CancelBookingCommand, Result<string>>
   {
        private readonly AppDbContext _context;
        public CancelBookingCommandHandler(AppDbContext context) {
            _context = context;
        }
        public async Task<Result<string>> Handle(CancelBookingCommand command, CancellationToken token)
        {
            var last_booking = await _context.bookings
                .Where(x => x.Status == Status.Pending)
                .FirstOrDefaultAsync(x => x.UserId == command.userId, token);

            if (last_booking == null)
                return Result<string>.Fail("Сначала запишитесь", Error.Conflict);

            last_booking.Status = Status.Cancelled;

            await _context.SaveChangesAsync(token);

            return Result<string>.Success($"Запись #{last_booking.Id} отменена");
        }
   }
}