using Telegram.Bot.Types;
using Telegram.Bot;
namespace TelegramBot.Interfaces
{
    public interface IBotHandler
    {
        bool CanHandle(Update update);   
        Task HandleAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken);
    }
}
