using MediatR;
using Infrastructure.Responses;
using Application.NailServices.DTO;
namespace Application.NailServices
{
    public record GetNailServiceQuery(Ulid ServiceId) : IRequest<Result<NailServiceResponse>>;
}