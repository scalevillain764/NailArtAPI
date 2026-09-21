using System.Text.Json.Serialization;

namespace TelegramBot.BotFlows 
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BotFlow { CreateUser, EditUser, CreateBooking, CancelBooking /*(cancel)*/};
}