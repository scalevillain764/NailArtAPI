using Application.Bookings.DTO;
using Application.Clients;
using Application.NailServices;
using MediatR;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBot.Buttons;
using TelegramBot.BotFlows;
using TelegramBot.DTO.Bookings;
using TelegramBot.DTO.Clients;
using TelegramBot.Interfaces;
using TelegramBot.StateMachines.Bookings;
using TelegramBot.StateMachines.Clients;
using IBotHandler = TelegramBot.Interfaces.IBotHandler;
using IRedisService = TelegramBot.Interfaces.IRedisService;
using Telegram.Bot.Types.ReplyMarkups;
namespace TelegramBot.BotHandlers
{
    public class CallbackQueryHandler : IBotHandler
    {
        private readonly IRedisService _redisService;
        private readonly IMediator _mediator;
        private readonly IEnumerable<IOperationHandler> _handlers;
        public CallbackQueryHandler(IRedisService redisService, IMediator mediator, IEnumerable<IOperationHandler> handlers)
        {
            _redisService = redisService;
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

                        var process = await _redisService.GetProcessAsync<ContactProcess>(userId);
                        var draft = await _redisService.GetDraftAsync<ShareContactDraft>(userId);

                        if(process != null)
                        {
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

                        await Task.WhenAll([
                            _redisService.SaveProcessAsync(userId, newProcess),
                            _redisService.SaveDraftAsync(userId, draft),
                            botClient.SendMessage(chatId, "Пожалуйста, введите имя", cancellationToken: cancellationToken)
                            ]);
          
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

                        var newProcess = new ContactProcess(ContactState.EnterName, BotFlow.EditUser, EditContactType.Name);
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

                        var newProcess = new ContactProcess(ContactState.EnterPhone, BotFlow.EditUser, EditContactType.Phone);
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

                        var newProcess = new ContactProcess(ContactState.EnterUserName, BotFlow.EditUser, EditContactType.UserName);
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
                case "contact:confirm":
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

                        process.EditType = EditContactType.Name;
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

                        process.EditType = EditContactType.Phone;
                        process.State = ContactState.EnterPhone;

                        var serializedProcess = JsonSerializer.Serialize(process);

                        await Task.WhenAll([
                              _redis.StringSetAsync($"contact:process:{userId}", serializedProcess),
                              botClient.SendMessage(chatId, "Введите новый номер телефона")
                            ]);

                        break;
                    }
                case "booking:add": // REFACTOR LATER
                    {
                        var results = await Task.WhenAll(
                            _redis.StringGetAsync($"booking:process:{userId}"),
                            _redis.StringGetAsync($"booking:creation_draft:{userId}")
                        );

                        var cachedProcess = results[0];
                        var cachedDraft = results[1];

                        if (!cachedProcess.IsNullOrEmpty) // незаконченное создание
                        {
                            var process = JsonSerializer.Deserialize<BookingProcess>(
                                (string)cachedProcess!);

                            if (process == null)
                                return;

                            string text = process.State switch
                            {
                                BookingState.SelectService => "услугу",
                                BookingState.SelectDay => "день",
                                BookingState.SelectYear => "год",
                                BookingState.SelectMonth => "месяц",
                                BookingState.SelectTime => "время",
                                _ => throw new KeyNotFoundException("Неизвестное состояние")
                            };

                            await botClient.SendMessage(
                                chatId,
                                $"Похоже, в прошлый раз вы не закончили создание записи. Пожалуйста, выберите {text}");

                            if(process.State == BookingState.SelectService)
                            {
                                var getServicesR = await _mediator.
                                   Send(new GetNailServicesQuery(process.Pagination.CurrentPage, process.Pagination.PageSize));

                                if (!getServicesR.IsSuccess)
                                {
                                    await botClient.SendMessage(chatId, getServicesR.ErrorMessage!);
                                    return;
                                }

                                List<InlineKeyboardButton[]> buttons = new();

                                var srvs = getServicesR.Context!.Items;

                                StringBuilder stringb = new StringBuilder();

                                stringb.Append($"Страница: {process.Pagination.CurrentPage}/{(int)Math.Ceiling((double)getServicesR.Context.TotalCount
                                    / process.Pagination.PageSize)}\nТекущие услуги:\n");

                                foreach (var s in srvs)
                                {
                                    stringb.Append($"{s.Name}\n{s.ShortDescription}\nДлительность: {s.DurationMinutes}\n{s.Price} BYN\n\n");
                                    buttons.Add(new[] { ButtonBuilder.Create(s.Name, $"service:{s.Id}") });
                                }

                                buttons.Add(new[] {ButtonBuilder.Create("<-", "button:choose_service:prev_page"), 
                                    ButtonBuilder.Create("->", "button:choose_service:next_page") });

                                var inlineKeyboard = new InlineKeyboardMarkup(buttons.ToArray()); // все 3 в столбик

                                var msg = await botClient.SendMessage(chatId, stringb.ToString(), replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);
                                process.MessageWithServicesId = msg.Id;

                                await _redis.StringSetAsync(
                                    $"booking:process:{userId}", JsonSerializer.Serialize(process));
                            }

                            return;
                        }

                        var newProcess = new BookingProcess(BotFlow.CreateBooking, BookingState.SelectService, null, null);
                        var newDraft = new ShareContactDraft(userId, null, null, callback.From.Username);

                        var getServicesRequest = await _mediator.
                                   Send(new GetNailServicesQuery(newProcess.Pagination.CurrentPage, newProcess.Pagination.PageSize));

                        if (!getServicesRequest.IsSuccess)
                        {
                            await botClient.SendMessage(chatId, getServicesRequest.ErrorMessage!);
                            return;
                        }

                        var services = getServicesRequest.Context!.Items;

                        List<InlineKeyboardButton[]> btns = new();

                        StringBuilder sb = new StringBuilder();

                        sb.Append($"Страница: {newProcess.Pagination.CurrentPage}/{(int)Math.Ceiling((double)getServicesRequest.Context.TotalCount
                            / newProcess.Pagination.PageSize)}\nТекущие услуги:\n");

                        foreach (var s in services)
                        {
                            sb.Append($"{s.Name}\n{s.ShortDescription}\nДлительность: {s.DurationMinutes}\n{s.Price} BYN\n\n");
                            btns.Add(new[] { ButtonBuilder.Create(s.Name, $"service:{s.Id}") });
                        }

                        btns.Add(new[] {ButtonBuilder.Create("<-", "button:choose_service:prev_page"),
                            ButtonBuilder.Create("->", "button:choose_service:next_page") });

                        var inlineK = new InlineKeyboardMarkup(btns.ToArray()); // все в столбик

                        var message = await botClient.SendMessage(chatId, sb.ToString(), replyMarkup: inlineK, cancellationToken: cancellationToken);
                        newProcess.MessageWithServicesId = message.Id;

                        await Task.WhenAll(
                            _redis.StringSetAsync(
                                $"booking:process:{userId}",
                                JsonSerializer.Serialize(newProcess)),

                            _redis.StringSetAsync(
                                $"booking:creation_draft:{userId}",
                                JsonSerializer.Serialize(newDraft))
                        );
                     
                        break;
                    }
            }
        }
    }
}
