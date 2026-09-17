using MediatR;
using Application.NailServices.DTO;
using Infrastructure.Responses;
namespace Application.NailServices
{
    public record RemoveNailServiceCommand(Ulid nailServiceId) : IRequest<Result<string>>;
}