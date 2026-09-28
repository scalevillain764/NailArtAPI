using TelegramBot.BotFlows;
using TelegramBot.StateMachines.Bookings;
using TelegramBot.DTO.Bookings;
namespace TelegramBot.DTO.Bookings
{
    public class BookingProcess {
        public BookingState State { get; set; }
        public EditBookingType? EditType { get; set; }
        public int? MessageWithServicesId { get; set; }
        public PaginationDTO Pagination { get; set; }
        public BookingProcess(BookingState state, int? messageWithServicesId, EditBookingType? editBookingType)
        {
            State = state;
            MessageWithServicesId = messageWithServicesId;
            EditType = editBookingType;
            Pagination = new PaginationDTO(1, 3, 0);
        }
    }
}