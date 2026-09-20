using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
namespace TelegramBot.Interfaces
{
    public interface IMUpdateHandler
    {
        Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken);
        Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken);
    }
}
