using MediatR;
using Infrastructure.Responses;
using Application.Clients.DTO;
namespace Application.Clients
{
    public record GetClientByIdQuery(long Id) : IRequest<Result<ClientResponse>>;
}