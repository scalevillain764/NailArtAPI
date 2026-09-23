using MediatR;
using Infrastructure.Responses;
namespace Application.TimeSlots
{
    public record GetFreeSlotsQuery(DateTime Date, Ulid ServiceId) : IRequest<Result<List<TimeOnly>>>;
}