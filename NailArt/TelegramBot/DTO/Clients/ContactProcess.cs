using System.Text.Json.Serialization;
using ContactState = TelegramBot.StateMachines.Clients.ContactState;
using TelegramBot.BotFlows;
using TelegramBot.StateMachines.Clients;
namespace TelegramBot.DTO.Clients
{
    public class ContactProcess
    {
        public ContactState State { get; set; }
        public EditContactType? EditType { get; set; } = null;
        public ContactProcess(ContactState state, EditContactType? editType)
        {
            State = state;
            EditType = editType;
        }
    }
}