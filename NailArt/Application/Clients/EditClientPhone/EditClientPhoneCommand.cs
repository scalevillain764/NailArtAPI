using MediatR;
using Infrastructure.Responses;
using Application.Clients.DTO;
namespace Application.Clients
{
    public record EditClientPhoneCommand(long userId, string newPhone) : IRequest<Result<ClientResponse>>;
}