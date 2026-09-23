using TelegramBot.BotFlows;
using TelegramBot.StateMachines.Bookings;
using TelegramBot.DTO.Bookings;
namespace TelegramBot.DTO.Bookings
{
    public class BookingProcess {
        public BotFlow Flow { get; set; }
        public BookingState State { get; set; }
        public int MessageWithServicesId { get; set; }
        public PaginationDTO Pagination { get; set; }
        public BookingProcess(BotFlow flow, BookingState state, int messageWithServicesId)
        {
            Flow = flow;
            State = state;
            MessageWithServicesId = messageWithServicesId;
            Pagination = new PaginationDTO(0, 3, 0);
        }
    }
}