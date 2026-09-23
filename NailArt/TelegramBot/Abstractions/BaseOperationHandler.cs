using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBot.BotFlows;
using TelegramBot.Interfaces;
namespace TelegramBot.Abstractions
{
    public abstract class BaseOperationHandler<TProcess, TEvent> : IOperationHandler<TProcess, TEvent>
    {
        public abstract bool CanHandleAndConfirm(BotFlow flow);
        public abstract Task HandleAsync(long userId, TEvent _event, TProcess process, ITelegramBotClient botClient, CancellationToken token);
        public abstract Task ConfirmAsync(long userId, long chatId, TProcess process, ITelegramBotClient botClient, CancellationToken token);

        Task IOperationHandler.HandleAsync(long userId, object _event, object process, ITelegramBotClient botClient, CancellationToken token)
        {
            if (process is TProcess typedProcess && _event is TEvent typedEvent)
                return HandleAsync(userId, typedEvent, typedProcess, botClient, token);
            throw new ArgumentException($"Хэндлер ожидал тип {typeof(TProcess).Name}, но получил {process?.GetType().Name}\n" +
                $"Хэндлер ожидал тип {typeof(TEvent).Name}, но получил {process?.GetType().Name}");
        }

        Task IOperationHandler.ConfirmAsync(long userId, long chatId, object process, ITelegramBotClient botClient, CancellationToken token)
        {
            if (process is TProcess typedProcess)
                return ConfirmAsync(userId, chatId, typedProcess, botClient, token);
            throw new ArgumentException($"Хэндлер ожидал тип {typeof(TProcess).Name}, но получил {process?.GetType().Name}");
        }
    }
}