namespace TimeEntry.ApiService.Apis;

/// <summary> One page of a list: its number, the page size actually used (a larger one that was asked for is lowered to the cap), how many rows match in all, and the rows. </summary>
public class PaginatedItems<TEntity>(int pageIndex, int pageSize, long count, IEnumerable<TEntity> data) where TEntity : class
{
    public int PageIndex { get; } = pageIndex;
    public int PageSize { get; } = pageSize;
    public long Count { get; } = count;
    public IEnumerable<TEntity> Data { get; } = data;
}
