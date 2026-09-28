using Infrastructure.Responses;
using Telegram.Bot;
using Telegram.Bot.Types;
namespace TelegramBot.BotHandlers
{
    public class ErrorHandler
    {
        public async Task<bool> HandleAsync<T>(
            Result<T> result,
            long chatId,
            ITelegramBotClient botClient,
            CancellationToken token) where T : class
        {
            if (result.IsSuccess)
                return true;

            await botClient.SendMessage(
                chatId,
                result.ErrorMessage!,
                cancellationToken: token);

            return false;
        }
    }
}