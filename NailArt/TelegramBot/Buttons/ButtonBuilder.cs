using Application.Bookings.DTO;
using Application.NailServices.DTO;
using Telegram.Bot.Types.ReplyMarkups;
namespace TelegramBot.Buttons
{
    public class ButtonBuilder
    {
        public static InlineKeyboardButton Create(string buttonText, string callbackData)
            => InlineKeyboardButton.WithCallbackData(buttonText, callbackData);
        public static InlineKeyboardMarkup MenuKeyboard()
            => new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("👤Мой профиль", "my:profile")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("📅 Мои записи", "my:bookings")
                }
            });

        public static InlineKeyboardMarkup MyProfileKeyboard()
            => new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("✏️ Изменить имя", "contact:edit_name")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("📱 Изменить телефон", "contact:edit_phone")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("♻️ Удалить юзернейм", "contact:remove_userName")
                },
                new[]
                {
                      InlineKeyboardButton.WithCallbackData("🫆 Изменить юзернейм", "contact:edit_userName")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("В меню", "menu")
                }
            });

        public static InlineKeyboardMarkup BookingToMenuKeyboard()
            => new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("В меню", "menu")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("К записям", "my:bookings")
                }
            });

        public static InlineKeyboardMarkup ClientToMenuKeyboard()
            => new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("В меню", "menu")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Мой профиль", "my:profile")
                }
            });

        public static InlineKeyboardMarkup MyBookings()
              => new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("✏️ Записаться", "my:bookings:add")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("❌ Отменить последнюю запись", "my:bookings:cancel")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("В меню", "menu")
                }
            });
        
        public static InlineKeyboardMarkup NailServicePaginationKeyboard(IEnumerable<NailServiceResponse> services)
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

        public static InlineKeyboardMarkup BookingsPaginationKeyboard()
        {
            List<InlineKeyboardButton[]> btns = new();

            btns.Add(new[] { ButtonBuilder.Create("<-", "button:my:bookings:prev_page"),
                ButtonBuilder.Create("->", "button:my:bookings:next_page") });

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
                    InlineKeyboardButton.WithCallbackData("❌ Отмена", "contact:create:cancel:menu")
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
                    InlineKeyboardButton.WithCallbackData("❌ Отмена", "my:profile")
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
                    InlineKeyboardButton.WithCallbackData("💅 Изменить услугу", "booking:create:edit_service")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🕐 Изменить время", "booking:create:edit_time") },
                new[] 
                {
                    InlineKeyboardButton.WithCallbackData("❌ Отмена", "booking:create:cancel")
                }
        });
    }
}