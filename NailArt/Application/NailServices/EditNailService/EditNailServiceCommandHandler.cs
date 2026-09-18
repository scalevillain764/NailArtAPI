using MediatR;
using Infrastructure.Responses;
using Error = Domain.Enums.ErrorType;
using Application.NailServices.DTO;
using Infrastructure;
using Domain;
namespace Application.NailServices
{
    public class EditNailServiceCommandHandler : IRequestHandler<EditNailServiceCommand, Result<NailServiceResponse>>
    {
        private readonly AppDbContext _context;
        public EditNailServiceCommandHandler(AppDbContext context)
        {
            _context = context;
        }
        public async Task<Result<NailServiceResponse>> Handle(EditNailServiceCommand command, CancellationToken token)
        {
            var nailService = await _context.services
                .FindAsync(command.Id, token);

            if (nailService == null)
                return Result<NailServiceResponse>.Fail("Услуга не найдена", Error.NotFound);

            nailService.Name = command.Name;
            nailService.ShortDescription = command.ShortDescription;
            nailService.Price = command.Price;
            nailService.DurationMinutes = command.DurationMinutes;

            await _context.SaveChangesAsync(token);

            return Result<NailServiceResponse>.Success(new NailServiceResponse(nailService));
        }
    }
}