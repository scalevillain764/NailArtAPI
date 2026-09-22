using System.Text.Json.Serialization;
using ContactState = TelegramBot.StateMachines.ContactState;
using TelegramBot.BotFlows;
namespace TelegramBot.DTO
{
    public class ContactProcess
    {
        public ContactState State { get; set; }
        public BotFlow Flow { get; set; }
        public EditType? EditType { get; set; } = null;
        public ContactProcess(ContactState state, BotFlow flow, EditType? editType)
        {
            State = state;
            Flow = flow;
            EditType = editType;
        }
    }
}