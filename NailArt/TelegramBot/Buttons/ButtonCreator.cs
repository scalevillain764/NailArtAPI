using Application.NailServices.DTO;
using Telegram.Bot.Types.ReplyMarkups;
namespace TelegramBot.Buttons
{
    public class ButtonBuilder
    {
        public static InlineKeyboardButton Create(string buttonText, string callbackData)
            => InlineKeyboardButton.WithCallbackData(buttonText, callbackData);

        public static InlineKeyboardMarkup BookingServicePaginationKeyboard(IEnumerable<NailServiceResponse> services)
        {
            List<InlineKeyboardButton[]> btns = new();
            foreach (var s in services)
            {
                btns.Add(new[] { ButtonBuilder.Create(s.Name, $"service:{s.Id}") });
            }

            btns.Add(new[] { ButtonBuilder.Create("<-", "button:choose_service:prev_page"), 
                ButtonBuilder.Create("->", "button:choose_service:next_page") });

            return new InlineKeyboardMarkup(btns.ToArray());
        }

        public static InlineKeyboardMarkup ContactCreatingConfirmationKeyoard()
            => new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("✅ Подтвердить", "contact:confirm")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Изменить имя", "contact:create:edit_name")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Изменить номер телефона", "contact:create:edit_phone")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("❌ Отмена", "contact:cancel")
                }
            });

        public static InlineKeyboardMarkup ContactEditingConfirmationKeyboard()
            => new InlineKeyboardMarkup(new[]
            {
                new[]{
                    InlineKeyboardButton.WithCallbackData("✅ Подтвердить", "contact:confirm")
                },
                  new[]
                {
                    InlineKeyboardButton.WithCallbackData("❌ Отмена", "contact:cancel")
                }
            });

        public static InlineKeyboardMarkup BookingConfirmationKeyboard()
            => new InlineKeyboardMarkup(new[]
            {
                new[]
                {  
                    InlineKeyboardButton.WithCallbackData("✅ Подтвердить", "booking:confirm")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("💅 Изменить услугу", "booking:edit_service")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🕐 Изменить время", "booking:edit_time") },
                new[] 
                {
                    InlineKeyboardButton.WithCallbackData("❌ Отмена", "booking:cancel")
                }
        });
    }
}