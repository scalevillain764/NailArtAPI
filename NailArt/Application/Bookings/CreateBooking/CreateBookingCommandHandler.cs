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

            bool serviceExists = await _context.services
                .AnyAsync(x => x.Id == command.ServiceId, token);

            if(!serviceExists)
                return Result<BookingResponse>.Fail("Услуга не найдена", Error.NotFound);

            var newBooking = new Booking(command.UserId, command.ServiceId, command.Date);

            _context.bookings.Add(newBooking);

            await _context.SaveChangesAsync(token);

            await _context.Entry(newBooking).Reference(x => x.User).LoadAsync(token);
            await _context.Entry(newBooking).Reference(x => x.Service).LoadAsync(token);

            return Result<BookingResponse>.Success(new BookingResponse(newBooking));
        }
   }
}