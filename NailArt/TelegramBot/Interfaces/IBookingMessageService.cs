using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBot.BotHandlers;
using TelegramBot.DTO.Bookings;

namespace TelegramBot.Interfaces;

public interface IBookingMessageService
{
    Task<Message?> SendServicesAsync(ITelegramBotClient botClient, long chatId, BookingProcess process, CancellationToken cancellationToken);
}