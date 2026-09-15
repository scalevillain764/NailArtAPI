using MediatR;
using Infrastructure.Responses;
using Application.Clients.DTO;
namespace Application.Clients
{
    public record CreateClientCommand(long Id, string Name, string Phone, string? UserName) : IRequest<Result<ClientResponse>>;
}