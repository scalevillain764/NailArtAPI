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
    public class CallbackQueryHandler : IBotHandler
    {
        private readonly IDatabase _redis;
        private readonly IMediator _mediator;
        public CallbackQueryHandler(IConnectionMultiplexer connectionMultiplexer, IMediator mediator)
        {
            _redis = connectionMultiplexer.GetDatabase();
            _mediator = mediator;
        }
        public bool CanHandle(Update update) => update.CallbackQuery is not null;
        public async Task HandleAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var callback = update.CallbackQuery!;
            var userId = callback.From.Id;
            var chatId = callback.Message?.Chat.Id;

            if (chatId == null)
                return;

            var data = callback.Data;

            switch (data)
            {
                case "contact:add":
                    {
                        bool userExists = await _mediator.Send(
                            new CheckClientByIdQuery(userId),
                            cancellationToken);

                        if (userExists)
                        {
                            await botClient.SendMessage(
                                chatId,
                                "Ваш контакт уже есть в нашей базе данных");

                            return;
                        }

                        var results = await Task.WhenAll(
                            _redis.StringGetAsync($"contact:process:{userId}"),
                            _redis.StringGetAsync($"contact:draft:{userId}")
                        );

                        var cachedProcess = results[0];
                        var cachedDraft = results[1];

                        if (!cachedProcess.IsNullOrEmpty) // незаконченное создание
                        {
                            var process = JsonSerializer.Deserialize<ContactProcess>(
                                (string)cachedProcess!);

                            if (process == null)
                                return;

                            string text = process.State switch
                            {
                                ContactState.EnterName => "имя",
                                ContactState.EnterPhone => "номер телефона",
                                _ => throw new KeyNotFoundException("Неизвестное состояние")
                            };

                            await botClient.SendMessage(
                                chatId,
                                $"Похоже, в прошлый раз вы не закончили создание контакта. Пожалуйста, введите {text}");

                            return;
                        }

                        // Процесса нет → начинаем создание
                        var newProcess = new ContactProcess(ContactState.EnterName, BotFlow.CreateUser);

                        var newDraft = new ShareContactDraft(userId, null, null, callback.From.Username);

                        await Task.WhenAll(
                            _redis.StringSetAsync(
                                $"contact:process:{userId}",
                                JsonSerializer.Serialize(newProcess)),

                            _redis.StringSetAsync(
                                $"contact:draft:{userId}",
                                JsonSerializer.Serialize(newDraft))
                        );

                        await botClient.SendMessage(
                            chatId,
                            "Пожалуйста, введите имя");

                        break;
                    }
                case "contact:edit_name":
                    {
                        bool userExists = await _mediator.Send(new CheckClientByIdQuery(userId), cancellationToken);

                        if(!userExists)
                        {
                            await botClient.SendMessage(chatId, $"Сначала создайте контакт");
                            return;
                        }

                        var newEditDraft = new ContactProcess(ContactState.EnterName, BotFlow.EditUser);     
                        
                        var serializedDraft = JsonSerializer.Serialize(newEditDraft);
                        await _redis.StringSetAsync($"contact:{userId}", serializedDraft);
                        break;
                    }
                case "contact:edit_phone":
                    {
                        bool userExists = await _mediator.Send(new CheckClientByIdQuery(userId), cancellationToken);

                        if (!userExists)
                        {
                            await botClient.SendMessage(chatId, $"Сначала создайте контакт");
                            return;
                        }

                        var newEditDraft = new ContactProcess(ContactState.EnterPhone, BotFlow.EditUser);

                        var serializedDraft = JsonSerializer.Serialize(newEditDraft);
                        await _redis.StringSetAsync($"contact:{userId}", serializedDraft);
                        break;
                    }
                case "contact:remove_userName":
                    {
                        var rez = await _mediator.Send(new EditClientUserNameCommand(userId, null), cancellationToken);

                        if(!rez.IsSuccess)
                        {
                            await botClient.SendMessage(chatId, $"{rez.ErrorMessage!}");
                            return;
                        }

                        await _redis.KeyDeleteAsync($"contact:{userId}");
                        break;            
                    }
                case "contact:edit_userName":
                    {
                        bool userExists = await _mediator.Send(new CheckClientByIdQuery(userId), cancellationToken);

                        if (!userExists)
                        {
                            await botClient.SendMessage(chatId, $"Сначала создайте контакт");
                            return;
                        }

                        var newEditDraft = new ContactProcess(ContactState.EnterUserName, BotFlow.EditUser);

                        var serializedDraft = JsonSerializer.Serialize(newEditDraft);
                        await _redis.StringSetAsync($"contact:{userId}", serializedDraft);
                        break;
                    }
            }
        }
    }
}
