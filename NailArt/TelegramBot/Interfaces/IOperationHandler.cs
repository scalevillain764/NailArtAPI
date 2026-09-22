using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBot.BotFlows;
using TelegramBot.DTO;
using TelegramBot.StateMachines;
namespace TelegramBot.Interfaces
{
    public interface IOperationHandler
    {
        bool CanHandle(BotFlow flow);
        Task HandleAsync(long userId, Message message, ContactProcess process, ITelegramBotClient botClient, CancellationToken token);
        bool CanConfirm(BotFlow flow);
        Task ConfirmAsync(long userId, long chatId, ContactState state, ITelegramBotClient botClient, CancellationToken token);
    }
}