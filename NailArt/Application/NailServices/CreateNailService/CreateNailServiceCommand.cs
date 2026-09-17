using MediatR;
using Application.NailServices.DTO;
using Infrastructure.Responses;
namespace Application.NailServices
{
    public record CreateNailServiceCommand(string Name, string? ShortDescription, int DurationMinutes, decimal Price) :
        IRequest<Result<NailServiceResponse>>;
}