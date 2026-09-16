using Application.Clients.DTO;
using Domain;
using Infrastructure.Responses;
using MediatR;
using Error = Domain.Enums.ErrorType;
using IUserUpdater = Application.Interfaces.IUserUpdater;
namespace Application.Clients
{
    public class EditClientUserNameCommandHandler : IRequestHandler<EditClientUserNameCommand, Result<ClientResponse>>
    {
        private readonly IUserUpdater _userUpdater;
        public EditClientUserNameCommandHandler(IUserUpdater userUpdater)
        {
            _userUpdater = userUpdater;
        }
        public Task<Result<ClientResponse>> Handle(EditClientUserNameCommand command, CancellationToken token)
            => _userUpdater.UpdateClientAsync(command.userId, u => u.UserName = command.newUserName, token);
    }
}