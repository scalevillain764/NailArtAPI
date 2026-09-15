using Client = Domain.User;
namespace Application.Clients.DTO
{
    public record ClientResponse(long TgId, string Name, string Phone, string? UserName)
    {
        public ClientResponse(Client client) : this(client.Id, client.Name, client.Phone, client.UserName) { }
    }
}