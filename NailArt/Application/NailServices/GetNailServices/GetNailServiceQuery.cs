using MediatR;
using Infrastructure.Responses;
using Application.NailServices.DTO;
namespace Application.NailServices
{
    public record GetNailServicesQuery(int Page = 1, int PageSize = 1) : IRequest<Result<PagedResponse<NailServiceResponse>>>;
}