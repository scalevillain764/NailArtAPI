using Application.Bookings;
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
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.BotFlows;
using TelegramBot.Buttons;
using TelegramBot.DTO.Bookings;
using TelegramBot.DTO.Clients;
using TelegramBot.Interfaces;
using TelegramBot.StateMachines.Bookings;
using TelegramBot.StateMachines.Clients;
using static Telegram.Bot.TelegramBotClient;
using IBotHandler = TelegramBot.Interfaces.IBotHandler;
using IRedisService = TelegramBot.Interfaces.IRedisService;
namespace TelegramBot.BotHandlers
{
    public class CallbackQueryHandler : IBotHandler
    {
        private readonly ErrorHandler _errorHandler;
        private readonly MenuHandler _menuHandler;
        private readonly IBookingMessageService _bookingMessageService;
        private readonly IRedisService _redisService;
        private readonly IMediator _mediator;
        private readonly IEnumerable<IOperationHandler> _handlers;
        public CallbackQueryHandler(IBookingMessageService bookingMessageService,
            IRedisService redisService, 
            IMediator mediator, 
            IEnumerable<IOperationHandler> handlers,
            MenuHandler menuHandler,
            ErrorHandler errorHandler)
        {
            _bookingMessageService = bookingMessageService;
            _redisService = redisService;
            _mediator = mediator;
            _handlers = handlers;
            _menuHandler = menuHandler;
            _errorHandler = errorHandler;
        }
        public bool CanHandle(Update update) => update.CallbackQuery is not null;
        public async Task HandleAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var callback = update.CallbackQuery!;
            var userId = callback.From.Id;
            var chatId = callback.Message?.Chat.Id;

            if (chatId == null)
                return;

            var currentSession = await _redisService.GetCurrentSession(userId);
            if (currentSession == null)
                return;

            var data = callback.Data;

            switch (data)
            {
                // profile/contacts
                case "my:profile":
                    {
                        currentSession.currentFlow = BotFlow.ShowingProfile;
                        var result = await _mediator.Send(new GetClientByIdQuery(userId), cancellationToken);

                        if (!await _errorHandler.HandleAsync(result, (long)chatId, botClient, cancellationToken))
                            return;

                        await Task.WhenAll([
                            _menuHandler.DisplayProfileMenu((long)chatId, botClient, result.Context!, currentSession, cancellationToken),
                            _redisService.SaveCurrentSession(userId, currentSession)
                           ]); 

                        break;
                    }
                case "contact:add":
                    {
                        currentSession.currentFlow = BotFlow.CreateUser;

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

                        if (process != null)
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

                        var newProcess = new ContactProcess(ContactState.EnterName, null);
                        var newDraft = new ShareContactDraft(userId, null, null, callback.From.Username);

                        await Task.WhenAll([
                            _redisService.SaveProcessAsync(userId, newProcess),
                            _redisService.SaveDraftAsync(userId, newDraft),
                            _redisService.SaveCurrentSession(userId, currentSession),
                            botClient.SendMessage(chatId, "Пожалуйста, введите имя", cancellationToken: cancellationToken)
                            ]);

                        break;
                    }
                case "contact:edit_name":
                    {
                        bool userExists = await _mediator.Send(new CheckClientByIdQuery(userId), cancellationToken);

                        if (!userExists)
                        {
                            await botClient.SendMessage(chatId, $"Сначала создайте контакт", cancellationToken: cancellationToken);
                            return;
                        }

                        currentSession.currentFlow = BotFlow.EditUser;
                        var newProcess = new ContactProcess(ContactState.EnterName, EditContactType.Name);
                        var newDraft = new EditContactDraft(null, null, null);

                        await Task.WhenAll([
                            _redisService.SaveProcessAsync(userId, newProcess),
                            _redisService.SaveDraftAsync(userId, newDraft),
                            _redisService.SaveCurrentSession(userId, currentSession),
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

                        currentSession.currentFlow = BotFlow.EditUser;
                        var newProcess = new ContactProcess(ContactState.EnterPhone, EditContactType.Phone);
                        var newDraft = new EditContactDraft(null, null, null);

                        await Task.WhenAll([
                            _redisService.SaveProcessAsync(userId, newProcess),
                            _redisService.SaveDraftAsync(userId, newDraft),
                            _redisService.SaveCurrentSession(userId, currentSession),
                            botClient.SendMessage(chatId, "Введите номер телефона", cancellationToken: cancellationToken)
                        ]);

                        break;
                    }
                case "contact:remove_userName":
                    {
                        var rez = await _mediator.Send(new EditClientUserNameCommand(userId, null), cancellationToken);

                        if (!rez.IsSuccess)
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

                        currentSession.currentFlow = BotFlow.EditUser;
                        var newProcess = new ContactProcess(ContactState.EnterUserName, EditContactType.UserName);
                        var newDraft = new EditContactDraft(null, null, null);

                        await Task.WhenAll([
                            _redisService.SaveProcessAsync(userId, newProcess),
                            _redisService.SaveDraftAsync(userId, newDraft),
                            _redisService.SaveCurrentSession(userId, currentSession),
                            botClient.SendMessage(chatId, "Введите юзер нейм", cancellationToken: cancellationToken)
                        ]);

                        break;
                    }
                case "contact:confirm":
                    {
                        var process = await _redisService.GetProcessAsync<ContactProcess>(userId);
                        if (process == null)
                        {
                            await botClient.SendMessage(chatId, "Что-то пошло не так", cancellationToken: cancellationToken);
                            return;
                        }

                        var handler = _handlers.FirstOrDefault(x => x.CanHandleAndConfirm(currentSession.currentFlow));
                        if (handler == null)
                        {
                            await botClient.SendMessage(chatId, "Что-то пошло не так", cancellationToken: cancellationToken);
                            return;
                        }

                        await handler.ConfirmAsync(userId, (long)chatId, process, botClient, currentSession, cancellationToken);

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

                        currentSession.currentFlow = BotFlow.CreateUser;
                        process.EditType = EditContactType.Name;
                        process.State = ContactState.EnterName;

                        await Task.WhenAll([
                              _redisService.SaveProcessAsync(userId, process),
                              _redisService.SaveCurrentSession(userId, currentSession),
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

                        currentSession.currentFlow = BotFlow.CreateUser;
                        process.EditType = EditContactType.Phone;
                        process.State = ContactState.EnterPhone;

                        var serializedProcess = JsonSerializer.Serialize(process);

                        await Task.WhenAll([
                              _redisService.SaveProcessAsync(userId, process),
                              _redisService.SaveCurrentSession(userId, currentSession),
                              botClient.SendMessage(chatId, "Введите новый номер телефона")
                            ]);

                        break;
                    }
                case "contact:create:cancel:menu":
                    {
                        await Task.WhenAll([
                            _redisService.DeleteProcessAsync<ContactProcess>(userId),
                            _redisService.DeleteDraftAsync<ShareContactDraft>(userId),
                            _menuHandler.DisplayMenu((long)chatId, false, botClient, currentSession, cancellationToken)
                            ]);
                        break;
                    }
                case "menu":
                    {
                        currentSession.currentFlow = BotFlow.Menu;
                        await _menuHandler.DisplayMenu((long)chatId, true, botClient, currentSession, cancellationToken);
                        break;
                    }    
                    
                // bookings
                case "my:bookings":
                    {
                        currentSession.currentFlow = BotFlow.ShowingBookings;
                        var result = await _mediator.Send(new GetBookingsToUserQuery(userId, currentSession.currentBookingsPage, 5), cancellationToken);

                        if (!await _errorHandler.HandleAsync(result, (long)chatId, botClient, cancellationToken))
                            return;

                        await Task.WhenAll([
                            _menuHandler.DisplayBookingsMenu((long)chatId, botClient, result.Context!, currentSession, cancellationToken),
                            _redisService.SaveCurrentSession(userId, currentSession)
                        ]);

                        break;
                    }
                case "booking:confirm":
                    {
                        var process = await _redisService.GetProcessAsync<BookingProcess>(userId);
                        if (process == null)
                        {
                            await botClient.SendMessage(chatId, "Что-то пошло не так", cancellationToken: cancellationToken);
                            return;
                        }

                        var handler = _handlers.FirstOrDefault(x => x.CanHandleAndConfirm(currentSession.currentFlow));
                        if (handler == null)
                        {
                            await botClient.SendMessage(chatId, "Что-то пошло не так", cancellationToken: cancellationToken);
                            return;
                        }

                        await handler.ConfirmAsync(userId, (long)chatId, process, botClient, currentSession, cancellationToken);

                        break;
                    }
                case "button:my:bookings:next_page":
                    {
                        var result = await _mediator.Send(new GetBookingsToUserQuery(userId, currentSession.currentBookingsPage, 5), cancellationToken);

                        if (!await _errorHandler.HandleAsync(result, (long)chatId, botClient, cancellationToken))
                            return;

                        var totalPages = (int)Math.Ceiling(
                            (double)result.Context!.TotalCount / result.Context!.PageSize);

                        _menuHandler.NextBookingsPage(currentSession, totalPages);

                        break;
                    }
                case "booking:create:edit_service":
                    {
                        var process = await _redisService.GetProcessAsync<BookingProcess>(userId);
                        if (process == null)
                        {
                            await botClient.SendMessage(chatId, $"Такого черновика нет", cancellationToken: cancellationToken);
                            return;
                        }

                        currentSession.currentFlow = BotFlow.CreateBooking;
                        process.EditType = EditBookingType.Service;
                        process.State = BookingState.SelectService;

                        var servicesMessage = await _bookingMessageService.SendServicesAsync(
                            botClient, (long)chatId, process, cancellationToken);

                        if (servicesMessage == null)
                            return;

                        process.MessageWithServicesId = servicesMessage.Id;

                        await Task.WhenAll([
                            _redisService.SaveProcessAsync(userId, process),
                            _redisService.SaveCurrentSession(userId, currentSession)
                            ]);

                        break;
                    }
                case "booking:edit_time":
                    {
                        var process = await _redisService.GetProcessAsync<BookingProcess>(userId);
                        if (process == null)
                        {
                            await botClient.SendMessage(chatId, $"Такого черновика нет", cancellationToken: cancellationToken);
                            return;
                        }

                        currentSession.currentFlow = BotFlow.CreateBooking;
                        process.EditType = EditBookingType.Year;
                        process.State = BookingState.SelectYear;

                        // send message wiuth years & buttons

                        await Task.WhenAll([
                            _redisService.SaveProcessAsync(userId, process),
                            _redisService.SaveCurrentSession(userId, currentSession)
                            ]);

                        break;
                    }
                case "booking:create:cancel": 
                    {
                        var result = await _mediator.Send(new GetBookingsToUserQuery(userId, currentSession.currentBookingsPage, 5), cancellationToken);

                        if (!await _errorHandler.HandleAsync(result, (long)chatId, botClient, cancellationToken))
                            return;

                        await Task.WhenAll([
                            _redisService.DeleteProcessAsync<BookingProcess>(userId),
                            _redisService.DeleteDraftAsync<CreateBookingDraft>(userId),
                            _menuHandler.DisplayBookingsMenu((long)chatId, botClient, result.Context!, currentSession, cancellationToken)
                           ]);

                        break;
                    }
                case "my:bookings:cancel":
                    {
                        var result = await _mediator.Send(new CancelBookingCommand(userId), cancellationToken);
                        string? text = null;

                        if (!result.IsSuccess)
                            text = result.ErrorMessage!;
                        else
                            text = "Вы успешно отменили запись";

                        await botClient.SendMessage(chatId, text, replyMarkup: ButtonBuilder.BookingToMenuKeyboard(), cancellationToken: cancellationToken);
                        break;
                    }
                case "button:my:bookings:prev_page":
                    {
                        _menuHandler.PrevBookingsPage(currentSession);
                        break;
                    }
                case "my:bookings:add": 
                    {
                        var process = await _redisService
                            .GetProcessAsync<BookingProcess>(userId);

                        currentSession.currentFlow = BotFlow.CreateBooking;

                        if (process != null)
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

                            if (process.State == BookingState.SelectService)
                            {
                                var message = await _bookingMessageService.SendServicesAsync(botClient, (long)chatId,
                                    process, cancellationToken);

                                if (message == null)
                                    return;

                                process.MessageWithServicesId = message.Id;

                                await _redisService.SaveProcessAsync(userId, process);
                            }

                            await _redisService.SaveCurrentSession(userId, currentSession);

                            return;
                        }

                        var newProcess = new BookingProcess(BookingState.SelectService, null, null);

                        var newDraft = new CreateBookingDraft(
                            userId, null, null, null,
                            null, null, null);

                        var servicesMessage = await _bookingMessageService.SendServicesAsync(
                            botClient, (long)chatId, newProcess, cancellationToken);

                        if (servicesMessage == null)
                            return;

                        newProcess.MessageWithServicesId = servicesMessage.Id;

                        await Task.WhenAll([
                            _redisService.SaveProcessAsync(userId, newProcess),
                            _redisService.SaveDraftAsync(userId, newDraft),
                            _redisService.SaveCurrentSession(userId, currentSession)
                            ]);

                        break;
                    }
            }
        }
    }
}
