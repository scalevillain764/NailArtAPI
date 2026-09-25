using Application.NailServices.DTO;
using Infrastructure;
using Infrastructure.Responses;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
namespace Application.NailServices
{
    public class GetNailServiceQueryHandler : IRequestHandler<GetNailServiceQuery, Result<NailServiceResponse>>
    {
        private readonly AppDbContext _context;
        public GetNailServiceQueryHandler(AppDbContext context)
        {
            _context = context;
        }
        public async Task<Result<NailServiceResponse>> Handle(GetNailServiceQuery query, CancellationToken token)
        {
            var rez = await _context.services
                .FindAsync(query.ServiceId);

            if (rez == null)
                return Result<NailServiceResponse>.Fail("Услуга не найдена", Domain.Enums.ErrorType.NotFound);

            return Result<NailServiceResponse>.Success(new NailServiceResponse(rez));
        }
    }
}