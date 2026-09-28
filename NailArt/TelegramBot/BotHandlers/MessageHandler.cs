using Application.Clients;
using MediatR;
using StackExchange.Redis;
using System.Drawing;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBot.DTO.Clients;
using TelegramBot.StateMachines;
using IBotHandler = TelegramBot.Interfaces.IBotHandler;
using TelegramBot.BotFlows;
using TelegramBot.Interfaces;
using TelegramBot.BotSessions;
namespace TelegramBot.BotHandlers
{
    public class MessageHandler : IBotHandler
    {
        private readonly IRedisService _redis;
        private readonly IMediator _mediator;
        private readonly IEnumerable<IOperationHandler> _handlers;
        public MessageHandler(IRedisService redis, IMediator mediator, IEnumerable<IOperationHandler> handlers)
        {
            _redis = redis;
            _mediator = mediator;
            _handlers = handlers;
        }
        public bool CanHandle(Update update) => update.Message is not null && update.Message.Text is not null;
        public async Task HandleAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            string message = update.Message!.Text!;
            var userId = update.Message.From?.Id;
            var chatId = update.Message?.Chat.Id;

            if (chatId == null)
                return;

            if (message == "/start")
            {
                var currentSession = await _redis.GetCurrentSession((long)userId);

                if (currentSession != null)
                {
                    await botClient.SendMessage(
                        chatId,
                        "Вы уже начали работу с ботом.",
                        cancellationToken: cancellationToken);

                    return;
                }

                var newSession = new BotSession(BotFlow.Menu);

                await _redis.SaveCurrentSession((long)userId, newSession);

                return;
            }

            var session = await _redis.GetCurrentSession((long)userId);

            if (currentSession == null)
            {
                await botClient.SendMessage(
                    chatId,
                    "Пожалуйста, сначала нажмите /start",
                    cancellationToken: cancellationToken);

                return;
            }

            switch (session.currentFlow)
            {
                case BotFlow.CreateUser:
                    {
                        var handler = _handlers.FirstOrDefault(x => x.CanHandleAndConfirm(BotFlow.CreateUser));
                        if (handler == null)
                        {
                            await botClient.SendMessage(chatId, $"Не удалось создать контакт ❌");
                            return;
                        }

                        var process = await _redis.GetProcessAsync<ContactProcess>((long)userId);
                        if (process == null)
                            return;

                        await handler.HandleAsync((long)userId, update.Message!, process, botClient, cancellationToken);
                        break;
                    }
                case BotFlow.EditUser:
                    {
                        var handler = _handlers.FirstOrDefault(x => x.CanHandleAndConfirm(BotFlow.EditUser));
                        if (handler == null)
                        {
                            await botClient.SendMessage(chatId, $"Не удалось отредактировать контакт ❌");
                            return;
                        }

                        var process = await _redis.GetProcessAsync<ContactProcess>((long)userId);
                        if (process == null)
                            return;

                        await handler.HandleAsync((long)userId, update.Message!, process, botClient, cancellationToken);
                        break;
                    }
            }
        }
    }
}