using NailService = Domain.NailService;
namespace Application.NailServices.DTO
{
    public record NailServiceResponse(Ulid Id, string Name, string? ShortDescription, int DurationMinutes, decimal Price)
    {
        public NailServiceResponse(NailService nailService) :
            this(nailService.Id, nailService.Name, nailService.ShortDescription, nailService.DurationMinutes, nailService.Price)
        { }
    }
}