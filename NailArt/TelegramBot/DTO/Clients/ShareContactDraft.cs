using System.Text.Json.Serialization;
using ContactState = TelegramBot.StateMachines.Clients.ContactState;
using TelegramBot.BotFlows;
namespace TelegramBot.DTO.Clients
{
    public class ShareContactDraft
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? UserName { get; set; }
        public ShareContactDraft(long id, string? name, string? phone, string? username)
        {
            Id = id;
            Name = name;
            Phone = phone;
            UserName = username;
        }
    }
}