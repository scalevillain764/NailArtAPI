using MediatR;
using Infrastructure.Responses;
using Error = Domain.Enums.ErrorType;
using Application.NailServices.DTO;
using Infrastructure;
using Domain;
using Microsoft.EntityFrameworkCore;
namespace Application.NailServices
{
    public class SearchNailServiceQueryHandler : IRequestHandler<SearchNailServiceQuery, Result<PagedResponse<NailServiceResponse>>>
    {
        private readonly AppDbContext _context;
        public SearchNailServiceQueryHandler(AppDbContext context)
        {
            _context = context;
        }
        public async Task<Result<PagedResponse<NailServiceResponse>>> Handle(SearchNailServiceQuery query, CancellationToken token)
        {
            var q = _context.services
                .AsQueryable();

            if (query.Name != null) 
            {
                var words = query.Name
                    .ToLower()
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);

                foreach(var word in words)
                {
                    q = q
                        .Where(x => x.Name
                            .ToLower()
                            .Contains(word));
                }
            }
            
            if(query.ShortDescription != null)
            {
                var words = query.ShortDescription
                    .ToLower()
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);

                foreach(var word in words)
                {
                    q = q
                        .Where(x => x.ShortDescription != null && 
                            x.ShortDescription
                            .ToLower()
                            .Contains(word));
                }
            }

            if(query.MinDurationMinutes != null)
                q = q
                    .Where(x => x.DurationMinutes >= query.MinDurationMinutes);

            if (query.MaxDurationMinutes != null)
                q = q
                    .Where(x => x.DurationMinutes <= query.MaxDurationMinutes);

            if(query.MinPrice != null)
                q = q
                    .Where(x => x.Price >= query.MinPrice);

            if (query.MaxPrice != null)
                q = q.Where(x => x.Price <= query.MaxPrice);

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