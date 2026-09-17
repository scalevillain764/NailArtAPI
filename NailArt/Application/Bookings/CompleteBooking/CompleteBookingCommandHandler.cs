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
   public class CompleteBookingCommandHandler : IRequestHandler<CompleteBookingCommand, Result<string>>
   {
        private readonly AppDbContext _context;
        public CompleteBookingCommandHandler(AppDbContext context) {
            _context = context;
        }
        public async Task<Result<string>> Handle(CompleteBookingCommand command, CancellationToken token)
        {
            var booking = await _context.bookings
                .FindAsync(command.bookingId, token);

            if (booking == null)
                return Result<string>.Fail("Бронь не найдена", Error.NotFound);

            booking.Status = Status.Completed;

            await _context.SaveChangesAsync(token);

            return Result<string>.Success($"Запись #{booking.Id} завершена успешно.");
        }
   }
}