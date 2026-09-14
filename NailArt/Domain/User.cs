using System.Net.Sockets;
using URole = Domain.Enums.UserRole;
namespace Domain
{
    public class User
    {
        public long Id { get; init; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string? UserName { get; set; }
        public List<Booking> bookings { get; set; } = new();
        public URole Role { get; init; }
        public User(long id, string name, string phone, string? userName, URole role)
        {
            Id = id;
            Name = name;
            Phone = phone;
            UserName = userName;
            Role = role;
        }
    }
}