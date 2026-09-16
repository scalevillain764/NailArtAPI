using Domain;
using Application.Clients.DTO;
using Infrastructure.Responses;
using Application.Services;
namespace Application.Interfaces
{
    public interface IUserUpdater
    {
        Task<Result<ClientResponse>> UpdateClientAsync(long userId, Action<User> updateAction, CancellationToken token);
    }
}
