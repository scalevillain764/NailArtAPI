using System.Text.Json.Serialization;

namespace TelegramBot.BotFlows 
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BotFlow { Menu, CreateUser, EditUser, CreateBooking, CancelBooking };
}