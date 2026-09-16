using Application.Clients.DTO;
using Domain;
using Infrastructure.Responses;
using MediatR;
using Error = Domain.Enums.ErrorType;
using IUserUpdater = Application.Interfaces.IUserUpdater;
namespace Application.Clients
{
    public class EditClientPhoneCommandHandler : IRequestHandler<EditClientPhoneCommand, Result<ClientResponse>>
    {
        private readonly IUserUpdater _userUpdater;
        public EditClientPhoneCommandHandler(IUserUpdater userUpdater)
        {
            _userUpdater = userUpdater;
        }
        public Task<Result<ClientResponse>> Handle(EditClientPhoneCommand command, CancellationToken token)
            => _userUpdater.UpdateClientAsync(command.userId, u => u.Phone = command.newPhone, token);
    }
}