using System.Text.Json.Serialization;

namespace TelegramBot.StateMachines
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ContactStateMachine {
        CreateEnterName,
        CreateEnterPhone, 
        EditEnterName, 
        EditEnterPhone, 
        EditEnterUserName };
}