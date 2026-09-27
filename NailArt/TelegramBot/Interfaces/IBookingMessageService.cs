using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBot.BotHandlers;
using TelegramBot.DTO.Bookings;

namespace TelegramBot.Interfaces;

public interface IBookingMessageService
{
    Task<Message?> UpdateServiceMessageAsync(ITelegramBotClient botClient, long chatId, int messageId, BookingProcess process, CancellationToken token);
    Task<Message?> SendServicesAsync(ITelegramBotClient botClient, long chatId, BookingProcess process, CancellationToken cancellationToken);
}