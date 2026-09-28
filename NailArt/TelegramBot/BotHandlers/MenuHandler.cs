using Application.Bookings.DTO;
using Application.Clients.DTO;
using Infrastructure.Responses;
using System.Diagnostics;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.BotSessions;
using TelegramBot.Buttons;
namespace TelegramBot.BotHandlers
{
    public class MenuHandler
    {
        public async Task DisplayMenu(long chatId, bool userExists, ITelegramBotClient botClient, BotSession session, CancellationToken cancellationToken)
        {
            if(session.currentFlow != BotFlows.BotFlow.Menu)
            {
                return;
            }

            if(userExists)
            {
                var menuKeyboard = ButtonBuilder.MenuKeyboard();
                await botClient.SendMessage(chatId, "🏠 Главное меню", replyMarkup: menuKeyboard, cancellationToken: cancellationToken);
                return;
            }
            else
            {
                var registrationKeyboard = new InlineKeyboardMarkup(new[] { new[]
                    {
                        ButtonBuilder.Create("Зарегистрироваться", "contact:add")
                    }
                });

                await botClient.SendMessage(chatId, "🏠 👋 Добро пожаловать!\n\nДля использования бота необходимо зарегистрироваться.", replyMarkup: registrationKeyboard, cancellationToken: cancellationToken);
                return;
            }
        }

        public async Task DisplayProfileMenu(long chatId, ITelegramBotClient botClient, ClientResponse client, BotSession session, CancellationToken cancellationToken)
        {
            if (session.currentFlow != BotFlows.BotFlow.ShowingProfile)
            {
                return;
            }

            var profileKeyboard = ButtonBuilder.MyProfileKeyboard();

            await botClient.SendMessage(chatId, $"{client.Name}\n{client.Phone}\n{client.UserName}", replyMarkup: profileKeyboard, cancellationToken: cancellationToken);
        }

        public async Task DisplayBookingsMenu(long chatId, ITelegramBotClient botClient, PagedResponse<BookingResponse> response, BotSession session, CancellationToken cancellationToken)
        {
            if (session.currentFlow != BotFlows.BotFlow.ShowingBookings)
            { 
                return;
            }

            var paginationKeyboard = ButtonBuilder.BookingsPaginationKeyboard();

            StringBuilder sb = new StringBuilder();

            var totalPages = (int)Math.Ceiling(
                (double)response.TotalCount / response.PageSize);

            sb.Append($"Ваши заявки:\nСтраница {response.Page}/{totalPages}");

            foreach(var r in response.Items)
            {
                sb.Append($"Запись №{r.BookingId}\n{r.ServiceName} - {r.DurationHours}ч.{r.DurationMinutes}м. - ")
                    .Append($"{r.Date.ToString("HH:mm dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture)}\n{r.Price} BYN\n\n");
            }

            await botClient.SendMessage(chatId, sb.ToString(), replyMarkup: paginationKeyboard, cancellationToken: cancellationToken);
        }

        public void PrevBookingsPage(BotSession session)
        {
            if (session.currentBookingsPage > 1)
                session.currentBookingsPage--;
        }

        public void NextBookingsPage(BotSession session, int totalPages)
        {
            if (session.currentBookingsPage < totalPages)
                session.currentBookingsPage++;
        }
    }
}