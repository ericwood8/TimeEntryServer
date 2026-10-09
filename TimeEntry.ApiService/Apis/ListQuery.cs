using System.Linq.Expressions;

namespace TimeEntry.ApiService.Apis;

/// <summary>
/// The query string of a list endpoint (spec item E03): ?pageIndex=0&amp;pageSize=25&amp;sort=whenEntered:desc&amp;search=text.
/// With no pageIndex and no pageSize the endpoint answers the whole list as a plain array, as it always did; with either one it answers a
/// <see cref="PaginatedItems{TEntity}"/> holding that page and the total number of rows that match.
/// </summary>
public record ListQuery(int? PageIndex = null, int? PageSize = null, string? Sort = null, string? Search = null)
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 200;

    public bool IsPaged => PageIndex != null || PageSize != null;
}

/// <summary> The columns a list may be sorted by, by the name the caller uses (case does not matter), and the way to order by each. </summary>
public sealed class SortMap<T> : Dictionary<string, Func<IQueryable<T>, bool, IOrderedQueryable<T>>>
{
    public SortMap() : base(StringComparer.OrdinalIgnoreCase) { }

    public SortMap<T> Add<TKey>(string name, Expression<Func<T, TKey>> key)
    {
        this[name] = (rows, descending) => descending ? rows.OrderByDescending(key) : rows.OrderBy(key);
        return this;
    }
}

public static class ListQueries
{
    /// <summary>
    /// Filters by the search text, orders (the caller's sort, else <paramref name="defaultSort"/>; ties are broken by the key so pages do not overlap),
    /// and pages when asked. A bad sort name, page index or page size answers 400.
    /// </summary>
    public static async Task<IResult> ToResultAsync<TEntity, TDto>(
        this IQueryable<TEntity> rows, ListQuery query, SortMap<TEntity> sorts, string defaultSort, Expression<Func<TEntity, int>> key,
        Func<string, Expression<Func<TEntity, bool>>>? search, Func<TEntity, TDto> toDto,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? shape = null)
        where TEntity : class
        where TDto : class
    {
        if (query.PageIndex is < 0)
            return ApiProblems.Invalid("pageIndex must be 0 or more.");
        if (query.PageSize is < 1)
            return ApiProblems.Invalid("pageSize must be 1 or more.");

        if (search != null && !string.IsNullOrWhiteSpace(query.Search))
            rows = rows.Where(search(query.Search.Trim()));

        // sort=name or sort=name:desc (or :asc)
        string sortText = string.IsNullOrWhiteSpace(query.Sort) ? defaultSort : query.Sort.Trim();
        string name = sortText;
        bool descending = false;
        int colon = sortText.IndexOf(':');
        if (colon >= 0)
        {
            name = sortText[..colon].Trim();
            string direction = sortText[(colon + 1)..].Trim();
            if (direction.Equals("desc", StringComparison.OrdinalIgnoreCase))
                descending = true;
            else if (!direction.Equals("asc", StringComparison.OrdinalIgnoreCase))
                return ApiProblems.Invalid($"The sort direction must be asc or desc, not '{direction}'.");
        }
        if (!sorts.TryGetValue(name, out var order))
            return ApiProblems.Invalid($"Cannot sort by '{name}'. Use one of: {string.Join(", ", sorts.Keys.Order(StringComparer.OrdinalIgnoreCase))}.");

        IQueryable<TEntity> ordered = order(rows, descending).ThenBy(key);
        if (!string.IsNullOrWhiteSpace(query.Search))
            ordered = ordered.TagWith(TimeEntry.ApiService.Data.RowGoalInterceptor.Tag);   // see the interceptor: a search plus a sort needs the hint
        if (shape != null)
            ordered = shape(ordered);   // for example Include, applied to the rows that are read, not to the count

        if (!query.IsPaged)
            return TypedResults.Ok((await ordered.AsNoTracking().ToListAsync()).Select(toDto));

        int pageSize = Math.Min(query.PageSize ?? ListQuery.DefaultPageSize, ListQuery.MaxPageSize);
        int pageIndex = query.PageIndex ?? 0;
        long count = await rows.LongCountAsync();
        var page = await ordered.AsNoTracking().Skip(pageIndex * pageSize).Take(pageSize).ToListAsync();
        return TypedResults.Ok(new PaginatedItems<TDto>(pageIndex, pageSize, count, page.Select(toDto).ToList()));
    }


    /// <summary>
    /// The ids of the employees whose name contains the search text (a few hundred rows at most). A search over rows that belong to an employee filters on
    /// EmployeeId IN (...) with these, which the database answers from the EmployeeId index; joining Employee inside the filter let the optimizer choose a
    /// plan that took 20 seconds for a search with no match, sorted by employee name, over 100,000 rows.
    /// </summary>
    public static async Task<int[]> EmployeeIdsNamedAsync(this TimeEntryContext context, ListQuery query) =>
        string.IsNullOrWhiteSpace(query.Search)
            ? []
            : await context.Employee.AsNoTracking().Where(e => e.Name.Contains(query.Search.Trim())).Select(e => e.EmployeeId).ToArrayAsync();
}
