using System.Text.Json.Serialization;

namespace TelegramBot.StateMachines
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ContactState {
        EnterName,
        EnterPhone, 
        EnterUserName };
}