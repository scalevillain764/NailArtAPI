using Application.NailServices.DTO;
using Infrastructure.Responses;
using MediatR;
namespace Application.NailServices
{
    public record EditNailServiceCommand(Ulid Id, string Name, string? ShortDescription, int DurationMinutes, decimal Price) :
        IRequest<Result<NailServiceResponse>>;
}