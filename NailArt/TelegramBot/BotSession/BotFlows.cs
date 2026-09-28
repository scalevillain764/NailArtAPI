using System.Text.Json.Serialization;

namespace TelegramBot.BotFlows 
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BotFlow { Menu, ShowingProfile, ShowingBookings, CreateUser, EditUser, CreateBooking };
}