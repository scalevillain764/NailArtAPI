namespace TelegramBot.DTO.Clients
{
    public class EditContactTypeDraft
    {
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? UserName { get; set; }
        public EditContactTypeDraft(string? name, string? phone, string? userName)
        {
            Name = name;
            Phone = phone;
            UserName = userName;
        }
    }
}
