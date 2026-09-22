using Application.Clients;
using MediatR;
using StackExchange.Redis;
using System.Drawing;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBot.BotFlows;
using TelegramBot.DTO;
using TelegramBot.Interfaces;
using TelegramBot.StateMachines;
using IBotHandler = TelegramBot.Interfaces.IBotHandler;
namespace TelegramBot.BotHandlers
{
    public class CallbackQueryHandler : IBotHandler
    {
        private readonly IDatabase _redis;
        private readonly IMediator _mediator;
        private readonly IEnumerable<IOperationHandler> _handlers;
        public CallbackQueryHandler(IConnectionMultiplexer connectionMultiplexer, IMediator mediator, IEnumerable<IOperationHandler> handlers)
        {
            _redis = connectionMultiplexer.GetDatabase();
            _mediator = mediator;
            _handlers = handlers;
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
                            _redis.StringGetAsync($"contact:creation_draft:{userId}")
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

                        var newProcess = new ContactProcess(ContactState.EnterName, BotFlow.CreateUser, null);
                        var newDraft = new ShareContactDraft(userId, null, null, callback.From.Username);

                        await Task.WhenAll(
                            _redis.StringSetAsync(
                                $"contact:process:{userId}",
                                JsonSerializer.Serialize(newProcess)),

                            _redis.StringSetAsync(
                                $"contact:creation_draft:{userId}",
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

                        var newProcess = new ContactProcess(ContactState.EnterName, BotFlow.EditUser, EditType.Name);
                        var newEditDraft = new EditContactDraft(null, null, null);

                        var serializedProcess = JsonSerializer.Serialize(newProcess);
                        var serializedEditDraft = JsonSerializer.Serialize(newEditDraft);

                        await Task.WhenAll([
                            _redis.StringSetAsync($"contact:process:{userId}", serializedProcess),
                            _redis.StringSetAsync($"contact:edit_draft:{userId}", serializedEditDraft),
                            botClient.SendMessage(chatId, "Введите имя")
                        ]);

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

                        var newProcess = new ContactProcess(ContactState.EnterPhone, BotFlow.EditUser, EditType.Phone);
                        var newEditDraft = new EditContactDraft(null, null, null);

                        var serializedProcess = JsonSerializer.Serialize(newProcess);
                        var serializedEditDraft = JsonSerializer.Serialize(newEditDraft);

                        await Task.WhenAll([
                            _redis.StringSetAsync($"contact:process:{userId}", serializedProcess),
                            _redis.StringSetAsync($"contact:edit_draft:{userId}", serializedEditDraft),
                            botClient.SendMessage(chatId, "Введите номер телефона")
                        ]);

                        break;
                    }
                case "contact:remove_userName":
                    {
                        var rez = await _mediator.Send(new EditClientUserNameCommand(userId, null), cancellationToken);

                        if(!rez.IsSuccess)
                        {
                            await botClient.SendMessage(chatId, rez.ErrorMessage!);
                            return;
                        }

                        await _redis.KeyDeleteAsync($"contact:process:{userId}");
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

                        var newProcess = new ContactProcess(ContactState.EnterUserName, BotFlow.EditUser, EditType.UserName);
                        var newEditDraft = new EditContactDraft(null, null, null);

                        var serializedProcess = JsonSerializer.Serialize(newProcess);
                        var serializedEditDraft = JsonSerializer.Serialize(newEditDraft);

                        await Task.WhenAll([
                            _redis.StringSetAsync($"contact:process:{userId}", serializedProcess),
                            _redis.StringSetAsync($"contact:edit_draft:{userId}", serializedEditDraft),
                            botClient.SendMessage(chatId, "Введите юзер нейм")
                        ]);

                        break;
                    }         
                case "confirm":
                    {
                        var cachedProcess = await _redis.StringGetAsync($"contact:process:{userId}");
                        if(cachedProcess.IsNullOrEmpty)
                        {
                            await botClient.SendMessage(chatId, $"Такого черновика нет");
                            return;
                        }

                        var process = JsonSerializer.Deserialize<ContactProcess>((string)cachedProcess!);
                        if (process == null)
                        {
                            await botClient.SendMessage(chatId, $"Такого черновика нет");
                            return;
                        }

                        var handler = _handlers.FirstOrDefault(x => x.CanHandleAndConfirm(process.Flow));
                        if(handler == null)
                        {
                            await botClient.SendMessage(chatId, "Что-то пошло не так");
                            return;
                        }

                        await handler.ConfirmAsync(userId, (long)chatId, process, botClient, cancellationToken);

                        break;                  
                    }
                case "contact:create:edit_name":
                    {
                        var cachedProcess = await _redis.StringGetAsync($"contact:process:{userId}");
                        if (cachedProcess.IsNullOrEmpty)
                        {
                            await botClient.SendMessage(chatId, $"Такого черновика нет");
                            return;
                        }

                        var process = JsonSerializer.Deserialize<ContactProcess>((string)cachedProcess!);
                        if (process == null)
                        {
                            await botClient.SendMessage(chatId, $"Такого черновика нет");
                            return;
                        }

                        process.EditType = EditType.Name;
                        process.State = ContactState.EnterName;

                        var serializedProcess = JsonSerializer.Serialize(process);

                        await Task.WhenAll([
                              _redis.StringSetAsync($"contact:process:{userId}", serializedProcess),
                              botClient.SendMessage(chatId, "Введите новое имя")
                            ]);

                        break;                   
                    }
                case "contact:create:edit_phone":
                    {
                        var cachedProcess = await _redis.StringGetAsync($"contact:process:{userId}");
                        if (cachedProcess.IsNullOrEmpty)
                        {
                            await botClient.SendMessage(chatId, $"Такого черновика нет");
                            return;
                        }

                        var process = JsonSerializer.Deserialize<ContactProcess>((string)cachedProcess!);
                        if (process == null)
                        {
                            await botClient.SendMessage(chatId, $"Такого черновика нет");
                            return;
                        }

                        process.EditType = EditType.Phone;
                        process.State = ContactState.EnterPhone;

                        var serializedProcess = JsonSerializer.Serialize(process);

                        await Task.WhenAll([
                              _redis.StringSetAsync($"contact:process:{userId}", serializedProcess),
                              botClient.SendMessage(chatId, "Введите новый номер телефона")
                            ]);

                        break;
                    }          
            }
        }
    }
}
