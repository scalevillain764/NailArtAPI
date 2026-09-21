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
namespace TelegramBot.BotHandlers
{
    public class MessageHandler : IBotHandler
    {
        private readonly IDatabase _redis;
        private readonly IMediator _mediator;
        public MessageHandler(IConnectionMultiplexer connectionMultiplexer, IMediator mediator)
        {
            _redis = connectionMultiplexer.GetDatabase();
            _mediator = mediator;
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

                    }
            }

        }
    }
}