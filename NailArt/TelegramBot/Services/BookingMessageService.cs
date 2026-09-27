using Application.NailServices;
using MediatR;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBot.BotHandlers;
using TelegramBot.Buttons;
using TelegramBot.DTO.Bookings;
using TelegramBot.Interfaces;
using TelegramBot;
namespace TelegramBot.Services
{
    public class BookingMessageService : IBookingMessageService
    {
        private readonly IMediator _mediator;

        public BookingMessageService(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<Message?> SendServicesAsync(
            ITelegramBotClient botClient,
            long chatId,
            BookingProcess process,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetNailServicesQuery(
                    process.Pagination.CurrentPage,
                    process.Pagination.PageSize));

            if (!result.IsSuccess)
            {
                await botClient.SendMessage(
                    chatId,
                    result.ErrorMessage!,
                    cancellationToken: cancellationToken);

                return null;
            }

            var services = result.Context!.Items;

            var totalPages = (int)Math.Ceiling(
                (double)result.Context.TotalCount / process.Pagination.PageSize);

            var text = new StringBuilder();

            text.Append(
                $"Страница: {process.Pagination.CurrentPage}/{totalPages}\n" +
                "Текущие услуги:\n");

            foreach (var service in services)
            {
                text.Append(
                    $"{service.Name}\n" +
                    $"{service.ShortDescription}\n" +
                    $"Длительность: {service.DurationMinutes}\n" +
                    $"{service.Price} BYN\n\n");
            }

            var keyboard =
                ButtonBuilder.BookingServicePaginationKeyboard(services);

            return await botClient.SendMessage(
                chatId,
                text.ToString(),
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }
    }
}
