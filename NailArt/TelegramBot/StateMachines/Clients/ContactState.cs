using System.Text.Json.Serialization;

namespace TelegramBot.StateMachines.Clients
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ContactState {
        EnterName,
        EnterPhone, 
        EnterUserName,
        WaitingConfirmationUser};
}