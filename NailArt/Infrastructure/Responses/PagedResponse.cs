namespace Infrastructure.Responses
{
    public record PagedResponse<T>(
            IEnumerable<T> Items,
            int Page,
            int PageSize,
            int TotalCount);
}