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
    public class CheckClientByIdQueryHandler : IRequestHandler<CheckClientByIdQuery, bool>
    {
        private readonly AppDbContext _context;
        public CheckClientByIdQueryHandler(AppDbContext context)
        {
            _context = context;
        }
        public async Task<bool> Handle(CheckClientByIdQuery query, CancellationToken token)
            => await _context.users.AnyAsync(x => x.Id == query.Id, token);
    }
}