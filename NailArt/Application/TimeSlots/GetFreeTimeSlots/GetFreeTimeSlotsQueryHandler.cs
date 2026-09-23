using Infrastructure;
using Infrastructure.Responses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace Application.TimeSlots
{
    public class GetFreeTimeSlotsQueryHandler : IRequestHandler<GetFreeSlotsQuery, Result<List<TimeOnly>>>
    {
        private readonly AppDbContext _context;
        public GetFreeTimeSlotsQueryHandler(AppDbContext context)
        {
            _context = context;
        }
        public async Task<Result<List<TimeOnly>>> Handle(GetFreeSlotsQuery query, CancellationToken token)
        {
            var service = await _context.services
                .FirstOrDefaultAsync(x => x.Id == query.ServiceId, token);

            if (service == null)
                return Result<List<TimeOnly>>.Fail("Услуга не найдена", Domain.Enums.ErrorType.NotFound);

            var workDayStart = query.Date.Date.AddHours(9);
            var workDayEnd = query.Date.Date.AddHours(18);

            var bookings = await _context.bookings
                .Include(x => x.Service)
                .Where(x => x.Date.Date == query.Date.Date)
                .ToListAsync(token);

            var freeSlots = new List<TimeOnly>();

            for (
                var slotStart = workDayStart;
                slotStart < workDayEnd;
                slotStart = slotStart.AddMinutes(30))
            {
                var slotEnd = slotStart.AddMinutes(service.DurationMinutes);

                if (slotEnd > workDayEnd)
                    continue;

                var isBusy = bookings.Any(booking =>
                {
                    var bookingStart = booking.Date;
                    var bookingEnd = booking.Date.AddMinutes(
                        booking.Service!.DurationMinutes);

                    return bookingStart < slotEnd &&
                           bookingEnd > slotStart;
                });

                if (!isBusy)
                {
                    freeSlots.Add(TimeOnly.FromDateTime(slotStart));
                }
            }

            return Result<List<TimeOnly>>.Success(freeSlots);
        }
    }
}