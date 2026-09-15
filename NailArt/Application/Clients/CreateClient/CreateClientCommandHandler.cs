using Application.Clients.DTO;
using Domain.Enums;
using Infrastructure;
using Infrastructure.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using User = Domain.User;
namespace Application.Clients
{
    public class CreateClientCommandHandler : IRequestHandler<CreateClientCommand, Result<ClientResponse>>
    {
        private readonly AppDbContext _context;
        public CreateClientCommandHandler(AppDbContext context)
        {
            _context = context;
        }
        public async Task<Result<ClientResponse>> Handle(CreateClientCommand command, CancellationToken token)
        {
            var user = await _context.users.FindAsync(command.Id, token);

            if (user != null)
                return Result<ClientResponse>.Success(new ClientResponse(user));

            var newUser = new User(command.Id, command.Name, command.Phone, command.UserName, UserRole.Client);

            _context.users.Add(newUser);
            await _context.SaveChangesAsync(token);

            return Result<ClientResponse>.Success(new ClientResponse(newUser));
        }
    }
}