using Application.Bookings;
using Application.NailServices;
using Application.TimeSlots;
using MediatR;
using Microsoft.EntityFrameworkCore.Storage;
using StackExchange.Redis;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Abstractions;
using TelegramBot.BotFlows;
using TelegramBot.BotSessions;
using TelegramBot.Buttons;
using TelegramBot.DTO.Bookings;
using TelegramBot.Interfaces;
using TelegramBot.Services;
using TelegramBot.StateMachines.Bookings;
using IDatabase = StackExchange.Redis.IDatabase;

namespace TelegramBot.BotHandlers
{
    public class CreateBookingHandler : BaseOperationHandler<BookingProcess, CallbackQuery>
    {
        private readonly IRedisService _redis;
        private readonly IMediator _mediator;
        private readonly IBookingMessageService _messageService;

        public CreateBookingHandler(
            IBookingMessageService messageService,
            IRedisService redis,
            IMediator mediator
        )
        {
            _messageService = messageService;
            _redis = redis;
            _mediator = mediator;
        }

        public override bool CanHandleAndConfirm(BotFlow flow) => flow == BotFlow.CreateBooking;

        public override async Task ConfirmAsync(
            long userId,
            long chatId,
            BookingProcess process,
            ITelegramBotClient botClient,
            BotSession currentSession,
            CancellationToken token
        )
        {
            if (process.State != BookingState.WaitingForConfirmation)
            {
                await botClient.SendMessage(
                    chatId,
                    "Что-то пошло не так",
                    cancellationToken: token
                );
                return;
            }

            var draft = await _redis.GetDraftAsync<CreateBookingDraft>(userId);

            if (draft == null)
            {
                await botClient.SendMessage(
                    chatId,
                    $"Не удалось найти последний черновик",
                    cancellationToken: token
                );
                return;
            }

            var result = await _mediator.Send(
                new CreateBookingCommand(userId, (Ulid)draft.ServiceId!, (DateTime)draft.DateTime!),
                token
            );

            if (!result.IsSuccess)
            {
                await botClient.SendMessage(
                    chatId,
                    result.ErrorMessage ?? "Записаться не удалось❌",
                    cancellationToken: token
                );
                return;
            }

            currentSession.currentFlow = BotFlow.Menu;

            await Task.WhenAll([
                _redis.DeleteDraftAsync<CreateBookingDraft>(userId),
                _redis.DeleteProcessAsync<BookingProcess>(userId),
                _redis.SaveCurrentSession(userId, currentSession),
                botClient.SendMessage(chatId, "Вы успешно записались ✅", cancellationToken: token),
            ]);
        }

        public override async Task HandleAsync(
            long userId,
            CallbackQuery query,
            BookingProcess process,
            ITelegramBotClient botClient,
            CancellationToken token
        )
        {
            long chatId = query.Message!.Chat.Id;
            var Data = query.Data!;

            var draft = await _redis.GetDraftAsync<CreateBookingDraft>(userId);
            if (draft == null)
            {
                await botClient.SendMessage(
                    chatId,
                    $"Не удалось найти предыдущий черновик",
                    cancellationToken: token
                );
                return;
            }

            bool changeMessage = false;

            switch (process.State)
            {
                case BookingState.SelectService:
                {
                    if (process.MessageWithServicesId != null)
                    {
                        switch (Data)
                        {
                            case "button:choose_service:next_page":
                            {
                                if (process.Pagination.CurrentPage < process.Pagination.TotalPages)
                                {
                                    process.Pagination.CurrentPage++;
                                    changeMessage = true;
                                }
                                break;
                            }
                            case "button:choose_service:prev_page":
                            {
                                if (process.Pagination.CurrentPage > 1)
                                {
                                    changeMessage = true;
                                    process.Pagination.CurrentPage--;
                                }
                                break;
                            }
                        }

                        if (changeMessage)
                        {
                            var message = await _messageService.UpdateServiceMessageAsync(
                                botClient,
                                (long)chatId,
                                (int)process.MessageWithServicesId,
                                process,
                                token
                            );

                            if (message == null)
                                return;

                            process.MessageWithServicesId = message.Id;

                            await _redis.SaveProcessAsync(userId, process);
                        }
                    }

                    if (Data.StartsWith("service:"))
                    {
                        var serviceIdString = Data["service:".Length..];

                        if (!Ulid.TryParse(serviceIdString, out var serviceId))
                            return;

                        draft.ServiceId = serviceId;

                        process.State =
                            process.EditType is EditBookingType.Service
                                ? BookingState.WaitingForConfirmation
                                : BookingState.SelectYear;

                        var serviceRequest = await _mediator.Send(
                            new GetNailServiceQuery((Ulid)draft.ServiceId),
                            token
                        );

                        if (!serviceRequest.IsSuccess)
                        {
                            await botClient.SendMessage(chatId, serviceRequest.ErrorMessage!);
                            return;
                        }

                        var service = serviceRequest.Context!;

                        StringBuilder sb = new StringBuilder();

                        sb.Append($"Так выглядит ваша запись:\nКлиент №{draft.UserId}\n\nУслуга:")
                            .Append(
                                $"{service.Name}\n{service.ShortDescription}\nДлительность: {service.DurationMinutes}\n{service.Price} BYN\n\n"
                            )
                            .Append(
                                $"Дата и время: {draft!.DateTime!.Value.ToString("HH:mm dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture)}"
                            );

                        var confirmationKeyboard = ButtonBuilder.BookingConfirmationKeyboard();

                        await Task.WhenAll([
                            _redis.SaveDraftAsync(userId, draft),
                            _redis.SaveProcessAsync(userId, process),
                            botClient.SendMessage(
                                chatId,
                                process.State == BookingState.WaitingForConfirmation
                                    ? $"Так выглядит ваша запись: {sb.ToString()}"
                                    : "Выберите, пожалуйста, год",
                                replyMarkup: process.State == BookingState.WaitingForConfirmation
                                    ? confirmationKeyboard
                                    : null,
                                cancellationToken: token
                            ),
                        ]);
                    }
                    break;
                }
                case BookingState.SelectYear:
                {
                    if (Data.StartsWith("year:"))
                    {
                        var yearString = Data["year:".Length..];

                        if (!int.TryParse(yearString, out var year))
                        {
                            await botClient.SendMessage(chatId, "Выберите корректный год");
                            return;
                        }

                        draft.Year = year;

                        process.State = BookingState.SelectMonth;

                        await Task.WhenAll([
                            _redis.SaveDraftAsync(userId, draft),
                            _redis.SaveProcessAsync(userId, process),
                            botClient.SendMessage(
                                chatId,
                                "Выберите, пожалуйста, месяц",
                                cancellationToken: token
                            ),
                        ]);
                    }
                    break;
                }
                case BookingState.SelectMonth:
                {
                    if (Data.StartsWith("month:"))
                    {
                        var monthString = Data["month:".Length..];

                        if (!int.TryParse(monthString, out var month))
                        {
                            await botClient.SendMessage(chatId, "Выберите корректный месяц");
                            return;
                        }

                        draft.Month = month;

                        process.State = BookingState.SelectDay;

                        await Task.WhenAll([
                            _redis.SaveDraftAsync(userId, draft),
                            _redis.SaveProcessAsync(userId, process),
                            botClient.SendMessage(
                                chatId,
                                "Выберите, пожалуйста, день",
                                cancellationToken: token
                            ),
                        ]);
                    }
                    break;
                }
                case BookingState.SelectDay:
                {
                    if (Data.StartsWith("day:"))
                    {
                        var dayString = Data["day:".Length..];

                        if (!int.TryParse(dayString, out var day))
                        {
                            await botClient.SendMessage(chatId, "Выберите корректный месяц");
                            return;
                        }

                        int days = DateTime.DaysInMonth((int)draft.Year!, (int)draft.Month!);

                        if (day > days)
                        {
                            await botClient.SendMessage(chatId, "Выберите корректный день");
                            return;
                        }

                        draft.Day = day;

                        process.State = BookingState.SelectTime;

                        await Task.WhenAll([
                            _redis.SaveDraftAsync(userId, draft),
                            _redis.SaveProcessAsync(userId, process),
                            botClient.SendMessage(
                                chatId,
                                "Выберите, пожалуйста, время",
                                cancellationToken: token
                            ),
                        ]);
                    }
                    break;
                }
                case BookingState.SelectTime:
                {
                    var curDateTime = new DateTime(
                        (int)draft.Year!,
                        (int)draft.Month!,
                        (int)draft.Day!
                    );

                    var allTimeAtCurrentDay = await _mediator.Send(
                        new GetFreeSlotsQuery(curDateTime, (Ulid)draft.ServiceId!)
                    );

                    if (Data.StartsWith("time:"))
                    {
                        var timeString = Data["time:".Length..];
                        if (!TimeOnly.TryParse(timeString, out var time))
                        {
                            await botClient.SendMessage(chatId, "Выберите корректное время");
                            return;
                        }

                        var newDateTime = curDateTime.AddHours(time.Hour).AddMinutes(time.Minute);

                        if (newDateTime.Date < DateTime.UtcNow.Date)
                        {
                            await botClient.SendMessage(chatId, "Выберите корректный месяц");
                            return;
                        }

                        draft.DateTime = newDateTime;

                        process.State = BookingState.WaitingForConfirmation;

                        StringBuilder sb = new StringBuilder();

                        var serviceRequest = await _mediator.Send(
                            new GetNailServiceQuery((Ulid)draft.ServiceId),
                            token
                        );

                        if (!serviceRequest.IsSuccess)
                        {
                            await botClient.SendMessage(chatId, serviceRequest.ErrorMessage!);
                            return;
                        }

                        var service = serviceRequest.Context!;

                        var confirmationKeyboard = ButtonBuilder.BookingConfirmationKeyboard();

                        sb.Append($"Так выглядит ваша запись:\nКлиент №{draft.UserId}\n\nУслуга:")
                            .Append(
                                $"{service.Name}\n{service.ShortDescription}\nДлительность: {service.DurationMinutes}\n{service.Price} BYN\n\n"
                            )
                            .Append(
                                $"Дата и время: {draft!.DateTime!.Value.ToString("HH:mm dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture)}"
                            );

                        await Task.WhenAll([
                            _redis.SaveDraftAsync(userId, draft),
                            _redis.SaveProcessAsync(userId, process),
                            botClient.SendMessage(
                                chatId,
                                $"Так выглядит ваша запись: {sb.ToString()}",
                                replyMarkup: confirmationKeyboard,
                                cancellationToken: token
                            ),
                        ]);
                    }

                    break;
                }
            }
        }
    }
}
