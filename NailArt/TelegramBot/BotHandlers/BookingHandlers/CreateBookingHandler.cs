using Application.NailServices;
using MediatR;
using Microsoft.EntityFrameworkCore.Storage;
using StackExchange.Redis;
using System.Text;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBot.Abstractions;
using TelegramBot.BotFlows;
using TelegramBot.DTO.Bookings;
using TelegramBot.Interfaces;
using TelegramBot.StateMachines.Bookings;
using IDatabase = StackExchange.Redis.IDatabase;
namespace TelegramBot.BotHandlers
{
    public class CreateBookingHandler : BaseOperationHandler<BookingProcess, CallbackQuery>
    {
        private readonly IDatabase _redis;
        private readonly IMediator _mediator;
        public CreateBookingHandler(IConnectionMultiplexer redis, IMediator mediator)
        {
            _redis = redis.GetDatabase();
            _mediator = mediator;
        }
        public override bool CanHandleAndConfirm(BotFlow flow) => flow == BotFlow.CreateBooking;
        public override async Task HandleAsync(
            long userId,
            CallbackQuery query,
            BookingProcess process,
            ITelegramBotClient botClient,
            CancellationToken token
        )
        {
            long chatId = query.Message.Chat.Id;
            var Data = query.Data;

            var cachedDraft = await _redis.StringGetAsync($"booking:creation_draft:{userId}");
            if(cachedDraft.IsNullOrEmpty)
            {
                await botClient.SendMessage(chatId, $"Не удалось найти черновик");
                return;
            }

            var draft = JsonSerializer.Deserialize<CreateBookingDraft>((string)cachedDraft!);
            if(draft == null)
            {
                await botClient.SendMessage(chatId, $"Что-то пошло не так");
                return;
            }

            bool changeMessage = false;

            switch(process.State)
            {
                case BookingState.EnterService:
                    {
                        if(process.MessageWithServicesId != null)
                        {
                            switch(Data)
                            {
                                case "button:choose_service:next_page":
                                    {
                                        if(process.Pagination.CurrentPage < process.Pagination.TotalPages)
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
                                var getServicesRequest = await _mediator.
                                    Send(new GetNailServicesQuery(process.Pagination.CurrentPage, process.Pagination.PageSize));

                                if (!getServicesRequest.IsSuccess)
                                {
                                    await botClient.SendMessage(chatId, getServicesRequest.ErrorMessage!);
                                    return;
                                }

                                var services = getServicesRequest.Context.Items;

                                StringBuilder sb = new StringBuilder();

                                foreach (var s in services)
                                {
                                    sb.Append($"{s.Name}\n{s.ShortDescription}\nДлительность: {s.DurationMinutes}\n{s.Price} BYN\n\n");
                                }

                                string newText =
                                    $"Страница: {process.Pagination.CurrentPage}/{/*ceil*/(getServicesRequest.Context.TotalCount / process.Pagination.PageSize)}\n" +
                                    $"Текущие услуги:\n" +
                                    sb.ToString();

                                await botClient.EditMessageText(chatId, process.MessageWithServicesId, newText);
                            }                          
                        }
                        break;
                    }
            }
        }
    }
}
