using Application.Clients.DTO;
using Domain.Enums;
using Infrastructure;
using Infrastructure.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using User = Domain.User;
namespace Application.Clients
{
    public class GetClientByIdQueryHandler : IRequestHandler<GetClientByIdQuery, Result<ClientResponse>>
    {
        private readonly AppDbContext _context;
        public GetClientByIdQueryHandler(AppDbContext context)
        {
            _context = context;
        }
        public async Task<Result<ClientResponse>> Handle(GetClientByIdQuery query, CancellationToken token)
        {
            var user = await _context.users
                .FindAsync(query.Id, token);

            if (user == null)
                return Result<ClientResponse>.Fail("Пользователь не найден", ErrorType.NotFound);

            return Result<ClientResponse>.Success(new ClientResponse(user));
        }
    }
}