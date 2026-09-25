using Telegram.Bot.Types.ReplyMarkups;
namespace TelegramBot.Buttons
{
    public class ButtonBuilder
    {
        public static InlineKeyboardButton Create(string buttonText, string callbackData)
            => InlineKeyboardButton.WithCallbackData(buttonText, callbackData);
    }
}