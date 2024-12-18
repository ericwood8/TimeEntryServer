namespace TimeEntry.ApiService.Apis;

public class PaginatedItems<TEntity>(int pageIndex, long count, IEnumerable<TEntity> data) where TEntity : class
{
    public int PageIndex { get; } = pageIndex;
    public long Count { get; } = count;
    public IEnumerable<TEntity> Data { get; } = data;
}