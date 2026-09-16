using Application.Clients.DTO;
using Domain;
using Infrastructure.Responses;
using MediatR;
using Error = Domain.Enums.ErrorType;
using IUserUpdater = Application.Interfaces.IUserUpdater;
namespace Application.Clients
{
    public class EditClientNameCommandHandler : IRequestHandler<EditClientNameCommand, Result<ClientResponse>>
    {
        private readonly IUserUpdater _userUpdater;
        public EditClientNameCommandHandler(IUserUpdater userUpdater)
        {
            _userUpdater = userUpdater;
        }
        public Task<Result<ClientResponse>> Handle(EditClientNameCommand command, CancellationToken token)
            => _userUpdater.UpdateClientAsync(command.userId, u => u.Name = command.newName, token);
    }
}