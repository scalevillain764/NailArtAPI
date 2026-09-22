using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBot.BotFlows;
using TelegramBot.DTO;
using TelegramBot.StateMachines;
namespace TelegramBot.Interfaces
{
    public interface IOperationHandler
    {
        bool CanHandleAndConfirm(BotFlow flow);
        Task HandleAsync(long userId, Message message, ContactProcess process, ITelegramBotClient botClient, CancellationToken token);
        Task ConfirmAsync(long userId, long chatId, ContactProcess process, ITelegramBotClient botClient, CancellationToken token);
    }
}