using System.Text.Json.Serialization;
using ContactState = TelegramBot.StateMachines.ContactStateMachine;
using TelegramBot.BotFlows;
namespace TelegramBot.DTO
{
    public class ShareContactDraft
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? UserName { get; set; }
        public ContactState State { get; set; }
        public BotFlow Flow { get; set; }
        public ShareContactDraft(long id, string? name, string? phone, string? username, 
            ContactState state,
            BotFlow flow)
        {
            Id = id;
            Name = name;
            Phone = phone;
            UserName = username;
            State = state;
            Flow = flow;
        }
    }
}