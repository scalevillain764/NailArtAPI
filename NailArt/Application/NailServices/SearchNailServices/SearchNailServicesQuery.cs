using Application.NailServices.DTO;
using Infrastructure.Responses;
using MediatR;
namespace Application.NailServices
{
    public record SearchNailServiceQuery(string? Name, string? ShortDescription, 
        int? MinDurationMinutes, int? MaxDurationMinutes,
        decimal? MinPrice, decimal? MaxPrice,
        int Page, int PageSize, int totalCount) :
        IRequest<Result<PagedResponse<NailServiceResponse>>>;
}