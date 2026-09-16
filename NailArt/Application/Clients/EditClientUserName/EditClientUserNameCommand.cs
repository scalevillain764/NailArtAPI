using MediatR;
using Infrastructure.Responses;
using Application.Clients.DTO;
namespace Application.Clients
{
    public record EditClientUserNameCommand(long userId, string? newUserName) : IRequest<Result<ClientResponse>>;
}