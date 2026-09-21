using MediatR;
using Infrastructure.Responses;
using Application.Clients.DTO;
namespace Application.Clients
{
    public record CheckClientByIdQuery(long Id) : IRequest<bool>;
}