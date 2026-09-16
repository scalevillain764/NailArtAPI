using MediatR;
using Infrastructure.Responses;
using Application.Bookings.DTO;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Error = Domain.Enums.ErrorType;
using Domain;
namespace Application.Bookings
{
   public class CreateBookingCommandHandler : 
        IRequestHandler<CreateBookingCommand, Result<BookingResponse>>
   {
        private readonly AppDbContext _context;
        public CreateBookingCommandHandler(AppDbContext context)
        {
            _context = context;
        }
        public async Task<Result<BookingResponse>> Handle(CreateBookingCommand command, CancellationToken token)
        {
            bool userExists = await _context.users
                .AnyAsync(x => x.Id == command.UserId, token);

            if (!userExists)
                return Result<BookingResponse>.Fail("Пользователь не найден", Error.NotFound);

            bool anyPendingBookings = await _context.bookings
                .Where(x => x.UserId == command.UserId)
                .Where(x => x.Status == Domain.Enums.BookingStatus.Pending)
                .AnyAsync(x => x.Date > DateTime.UtcNow);

            if (anyPendingBookings)
                return Result<BookingResponse>.Fail("Может быт всего одна активная бронь", Error.Conflict);

            var service = await _context.services
                .FindAsync(command.ServiceId, token);

            if(service == null)
                return Result<BookingResponse>.Fail("Услуга не найдена", Error.NotFound);

            var newStart = command.Date;
            var newEnd = command.Date.AddMinutes(service.DurationMinutes);

            bool bookingAtCurrentDate = await _context.bookings
                .Include(x => x.Service)
                .Where(x => x.Status == Domain.Enums.BookingStatus.Pending)
                .Where(x => x.Date.Date == command.Date.Date)
                .AnyAsync(x => x.Date < newEnd && x.Date.AddMinutes(x.Service.DurationMinutes) > newStart, token);

            if (bookingAtCurrentDate)
                return Result<BookingResponse>.Fail("Выберите другое время для записи", Error.Forbidden);

            var newBooking = new Booking(command.UserId, command.ServiceId, command.Date);

            _context.bookings.Add(newBooking);

            await _context.SaveChangesAsync(token);

            await _context.Entry(newBooking).Reference(x => x.User).LoadAsync(token);
            await _context.Entry(newBooking).Reference(x => x.Service).LoadAsync(token);

            return Result<BookingResponse>.Success(new BookingResponse(newBooking));
        }
   }
}