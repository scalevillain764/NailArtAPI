using Application.Clients;
using MediatR;
using StackExchange.Redis;
using System.Drawing;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBot.DTO;
using TelegramBot.StateMachines;
using IBotHandler = TelegramBot.Interfaces.IBotHandler;
using TelegramBot.BotFlows;
using TelegramBot.Interfaces;
namespace TelegramBot.BotHandlers
{
    public class MessageHandler : IBotHandler
    {
        private readonly IDatabase _redis;
        private readonly IMediator _mediator;
        private readonly IEnumerable<IOperationHandler> _handlers;
        public MessageHandler(IConnectionMultiplexer connectionMultiplexer, IMediator mediator, IEnumerable<IOperationHandler> handlers)
        {
            _redis = connectionMultiplexer.GetDatabase();
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

            var cachedProcess = await _redis.StringGetAsync($"contact:process:{userId}");

            if(cachedProcess.IsNullOrEmpty)
            {
                await botClient.SendMessage(chatId, $"Что-то пошло не так");
                return;
            }

            var deserializedProcess = JsonSerializer.Deserialize<ContactProcess>((string)cachedProcess!);

            switch(deserializedProcess.Flow)
            {
                case BotFlow.CreateUser:
                    {
                        var handler = _handlers.FirstOrDefault(x => x.CanHandle(BotFlow.CreateUser));
                        if (handler == null)
                        {
                            await botClient.SendMessage(chatId, $"Не удалось создать контакт ❌");
                            return;
                        }
                        await handler.HandleAsync((long)userId, update.Message!, deserializedProcess, botClient, cancellationToken);
                        break;
                    }
                case BotFlow.EditUser:
                    {

                    }
            }
        }
    }
}