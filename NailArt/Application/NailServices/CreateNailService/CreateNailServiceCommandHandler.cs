using MediatR;
using Infrastructure.Responses;
using Error = Domain.Enums.ErrorType;
using Application.NailServices.DTO;
using Infrastructure;
using Domain;
namespace Application.NailServices
{
    public class CreateNailServiceCommandHandler : IRequestHandler<CreateNailServiceCommand, Result<NailServiceResponse>>
    {
        private readonly AppDbContext _context;
        public CreateNailServiceCommandHandler(AppDbContext context)
        {
            _context = context;
        }
        public async Task<Result<NailServiceResponse>> Handle(CreateNailServiceCommand command, CancellationToken token)
        {     
            var newNailService = new NailService(command.Name, command.ShortDescription, command.DurationMinutes, command.Price);

            _context.Add(newNailService);
            await _context.SaveChangesAsync(token);

            return Result<NailServiceResponse>.Success(new NailServiceResponse(newNailService));
        }
    }
}