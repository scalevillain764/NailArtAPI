using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBot.BotFlows;
using TelegramBot.DTO.Clients;
using TelegramBot.StateMachines.Clients;
using TelegramBot.BotSessions;
namespace TelegramBot.Interfaces
{
    public interface IOperationHandler
    {
        bool CanHandleAndConfirm(BotFlow flow);
        Task HandleAsync(long userId, object _event, object process, ITelegramBotClient botClient, CancellationToken token);
        Task ConfirmAsync(long userId, long chatId, object process, ITelegramBotClient botClient, BotSession currentSession, CancellationToken token);
    }


    public interface IOperationHandler<TProcess, TEvent> : IOperationHandler
    {
        Task HandleAsync(long userId, TEvent _event, TProcess process, ITelegramBotClient botClient, CancellationToken token);
        Task ConfirmAsync(long userId, long chatId, TProcess process, ITelegramBotClient botClient, BotSession currentSession, CancellationToken token);
    }

}