using Application.Clients.DTO;
using Domain;
using Infrastructure;
using Infrastructure.Responses;
using IUserUpdater = Application.Interfaces.IUserUpdater;
using Error = Domain.Enums.ErrorType;
namespace Application.Services
{
    public class UserUpdater : IUserUpdater
    {
        private readonly AppDbContext _context;
        public UserUpdater(AppDbContext context)
        {
            _context = context;
        }
        public async Task<Result<ClientResponse>> UpdateClientAsync(long userId, Action<User> updateAction, CancellationToken token)
        {
            var user = await _context.users
                .FindAsync(userId, token);

            if (user == null)
                return Result<ClientResponse>.Fail("Пользователь не найден", Error.NotFound);

            updateAction(user);

            await _context.SaveChangesAsync(token);

            return Result<ClientResponse>.Success(new ClientResponse(user));
        }
    }
}