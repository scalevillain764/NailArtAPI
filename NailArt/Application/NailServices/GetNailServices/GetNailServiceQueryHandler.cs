using MediatR;
using Infrastructure.Responses;
using Application.NailServices.DTO;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
namespace Application.NailServices
{
    public class GetNailServicesQueryHandler : IRequestHandler<GetNailServicesQuery, Result<PagedResponse<NailServiceResponse>>>
    {
        private readonly AppDbContext _context;
        public GetNailServicesQueryHandler(AppDbContext context)
        {
            _context = context;
        }
        public async Task<Result<PagedResponse<NailServiceResponse>>> Handle(GetNailServicesQuery query, CancellationToken token)
        {
            if (query.Page < 1 || query.PageSize < 1)
                return Result<PagedResponse<NailServiceResponse>>.Fail("Проверьте данные страницы", Domain.Enums.ErrorType.Validation);

            var q = _context.services
                .AsQueryable();

            int totalCount = await q.CountAsync(token);
            var rez = await q
                .Select(x => new NailServiceResponse(x))
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(token);

            return Result<PagedResponse<NailServiceResponse>>.Success(new PagedResponse<NailServiceResponse>(rez, query.Page, query.PageSize, totalCount));
        }
    }
}