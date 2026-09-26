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
                            _redisService.SaveDraftAsync(userId, newDraft),
                            botClient.SendMessage(chatId, "Пожалуйста, введите имя", cancellationToken: cancellationToken)
                            ]);
          
                        break;
                    }
                case "contact:edit_name":
                    {
                        bool userExists = await _mediator.Send(new CheckClientByIdQuery(userId), cancellationToken);

                        if(!userExists)
                        {
                            await botClient.SendMessage(chatId, $"Сначала создайте контакт", cancellationToken: cancellationToken);
                            return;
                        }

                        var newProcess = new ContactProcess(ContactState.EnterName, BotFlow.EditUser, EditContactType.Name);
                        var newDraft = new EditContactDraft(null, null, null);

                        await Task.WhenAll([
                            _redisService.SaveProcessAsync(userId, newProcess),
                            _redisService.SaveDraftAsync(userId, newDraft),
                            botClient.SendMessage(chatId, "Введите имя", cancellationToken: cancellationToken)
                        ]);

                        break;
                    }
                case "contact:edit_phone":
                    {
                        bool userExists = await _mediator.Send(new CheckClientByIdQuery(userId), cancellationToken);

                        if (!userExists)
                        {
                            await botClient.SendMessage(chatId, $"Сначала создайте контакт", cancellationToken: cancellationToken);
                            return;
                        }

                        var newProcess = new ContactProcess(ContactState.EnterPhone, BotFlow.EditUser, EditContactType.Phone);
                        var newDraft = new EditContactDraft(null, null, null);

                        await Task.WhenAll([
                            _redisService.SaveProcessAsync(userId, newProcess),
                            _redisService.SaveDraftAsync(userId, newDraft),
                            botClient.SendMessage(chatId, "Введите номер телефона", cancellationToken: cancellationToken)
                        ]);

                        break;
                    }
                case "contact:remove_userName":
                    {
                        var rez = await _mediator.Send(new EditClientUserNameCommand(userId, null), cancellationToken);

                        if(!rez.IsSuccess)
                        {
                            await botClient.SendMessage(chatId, rez.ErrorMessage!, cancellationToken: cancellationToken);
                            return;
                        }

                        await _redisService.DeleteProcessAsync<ContactProcess>(userId);
                        break;            
                    }
                case "contact:edit_userName":
                    {
                        bool userExists = await _mediator.Send(new CheckClientByIdQuery(userId), cancellationToken);

                        if (!userExists)
                        {
                            await botClient.SendMessage(chatId, $"Сначала создайте контакт", cancellationToken: cancellationToken);
                            return;
                        }

                        var newProcess = new ContactProcess(ContactState.EnterUserName, BotFlow.EditUser, EditContactType.UserName);
                        var newDraft = new EditContactDraft(null, null, null);

                        await Task.WhenAll([
                            _redisService.SaveProcessAsync(userId, newProcess),
                            _redisService.SaveDraftAsync(userId, newDraft),
                            botClient.SendMessage(chatId, "Введите юзер нейм", cancellationToken: cancellationToken)
                        ]);

                        break;
                    }         
                case "contact:confirm":
                    {
                        var process = await _redisService.GetProcessAsync<ContactProcess>(userId);
                        if(process == null)
                        {
                            await botClient.SendMessage(chatId, "Что-то пошло не так", cancellationToken: cancellationToken);
                            return;
                        }

                        var handler = _handlers.FirstOrDefault(x => x.CanHandleAndConfirm(process.Flow));
                        if(handler == null)
                        {
                            await botClient.SendMessage(chatId, "Что-то пошло не так", cancellationToken: cancellationToken);
                            return;
                        }

                        await handler.ConfirmAsync(userId, (long)chatId, process, botClient, cancellationToken);

                        break;                  
                    }
                case "contact:create:edit_name":
                    {
                        var process = await _redisService.GetProcessAsync<ContactProcess>(userId);
                        if (process == null)
                        {
                            await botClient.SendMessage(chatId, $"Такого черновика нет", cancellationToken: cancellationToken);
                            return;
                        }

                        process.EditType = EditContactType.Name;
                        process.State = ContactState.EnterName;

                        await Task.WhenAll([
                              _redisService.SaveProcessAsync(userId, process),
                              botClient.SendMessage(chatId, "Введите новое имя")
                            ]);

                        break;                   
                    }
                case "contact:create:edit_phone":
                    {
                        var process = await _redisService.GetProcessAsync<ContactProcess>(userId);
                        if (process == null)
                        {
                            await botClient.SendMessage(chatId, $"Такого черновика нет", cancellationToken: cancellationToken);
                            return;
                        }

                        process.EditType = EditContactType.Phone;
                        process.State = ContactState.EnterPhone;

                        var serializedProcess = JsonSerializer.Serialize(process);

                        await Task.WhenAll([
                              _redisService.SaveProcessAsync(userId, process),
                              botClient.SendMessage(chatId, "Введите новый номер телефона")
                            ]);

                        break;
                    }
                case "booking:add": // REFACTOR LATER
                    {
                        var process = await _redisService.GetProcessAsync<BookingProcess>(userId);
                        var draft = await _redisService.GetDraftAsync<CreateBookingDraft>(userId);

                        if (process != null) // незаконченное создание
                        {
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
                                $"Похоже, в прошлый раз вы не закончили создание записи. Пожалуйста, выберите {text}",
                                cancellationToken: cancellationToken);

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

                                await _redisService.SaveProcessAsync(userId, process);
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
                          _redisService.SaveProcessAsync(userId, newProcess),
                          _redisService.SaveDraftAsync(userId, newDraft)
                        );
                     
                        break;
                    }
            }
        }
    }
}
