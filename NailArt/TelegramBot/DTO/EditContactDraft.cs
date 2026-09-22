namespace TelegramBot.DTO
{
    public class EditContactDraft
    {
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? UserName { get; set; }
        public EditContactDraft(string? name, string? phone, string? userName)
        {
            Name = name;
            Phone = phone;
            UserName = userName;
        }
    }
}
