using MediatR;
using Infrastructure.Responses;
using Error = Domain.Enums.ErrorType;
using Application.NailServices.DTO;
using Infrastructure;
using Domain;
namespace Application.NailServices
{
    public class RemoveNailServiceCommandHandler : IRequestHandler<RemoveNailServiceCommand, Result<string>>
    {
        private readonly AppDbContext _context;
        public RemoveNailServiceCommandHandler(AppDbContext context)
        {
            _context = context;
        }
        public async Task<Result<string>> Handle(RemoveNailServiceCommand command, CancellationToken token)
        {
            var nailService = await _context.services
                 .FindAsync(command.nailServiceId, token);

            if (nailService == null)
                return Result<string>.Fail("Услуга не найдена", Error.NotFound);

            nailService.IsDeleted = true;

            await _context.SaveChangesAsync(token);

            return Result<string>.Success($"Услуга #{nailService.Id} удалена успешно");
        }
    }
}