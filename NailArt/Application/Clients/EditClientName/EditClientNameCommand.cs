using MediatR;
using Infrastructure.Responses;
using Application.Clients.DTO;
namespace Application.Clients
{
    public record EditClientNameCommand(long userId, string newName) : IRequest<Result<ClientResponse>>;
}