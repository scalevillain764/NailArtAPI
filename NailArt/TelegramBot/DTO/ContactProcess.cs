using System.Text.Json.Serialization;
using ContactState = TelegramBot.StateMachines.ContactState;
using TelegramBot.BotFlows;
namespace TelegramBot.DTO
{
    public class ContactProcess
    {
        public ContactState State { get; set; }
        public BotFlow Flow { get; set; }
        public ContactProcess(ContactState state, BotFlow flow)
        {
            State = state;
            Flow = flow;
        }
    }
}