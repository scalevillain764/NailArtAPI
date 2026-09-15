namespace Infrastructure.Responses
{
    public record PagedResponse<T>(
            IEnumerable<T> Items,
            int Pages,
            int PageSize,
            int TotalCount);
}