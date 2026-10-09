namespace TimeEntry.ApiService.Apis;

/// <summary>
/// A table whose rows have a name: the name is trimmed and must be letters, digits, spaces, apostrophes and hyphens (400 otherwise), and
/// GET /route/{name} finds the rows with that name (no match is an empty list). Whether a duplicate name is refused is the repository's rule
/// (NameActiveRepo answers 422 through <see cref="CrudApi{TEntity, TInput, TOutput}"/>; a holiday may share a name).
/// </summary>
public abstract class NamedCrudApi<TEntity, TInput, TOutput> : CrudApi<TEntity, TInput, TOutput>
    where TEntity : class, IHasName
    where TInput : class
    where TOutput : class
{
    protected abstract Task<List<TEntity>> FindByNameAsync(TimeEntryContext context, string name);

    protected override Task<IResult?> PrepareCreateAsync(CrudCall call, TEntity row, TInput input)
    {
        row.Name = (row.Name ?? "").Trim();
        return Task.FromResult(row.Name.IsNameBad() ? ApiProblems.BadName() : null);
    }

    protected override async Task<IResult?> PrepareUpdateAsync(CrudCall call, int id, TEntity row, TInput input)
    {
        row.Name = (row.Name ?? "").Trim();
        if (row.Name.IsNameBad())
            return ApiProblems.BadName();
        return await base.PrepareUpdateAsync(call, id, row, input);
    }

    protected override void RegisterExtras(IEndpointRouteBuilder app, string route, string singular, string plural)
    {
        app.MapGet(route + "/{name}", async (TimeEntryContext context, string name) =>
            {
                if (name.IsNameBad())
                    return ApiProblems.BadName();
                return TypedResults.Ok((await FindByNameAsync(context, name)).Select(ToOutput).ToList());
            })
            .WithName($"Get{singular}ByName")
            .Produces<List<TOutput>>()
            .ProducesProblem(400)
            .ProducesProblem(500);

        base.RegisterExtras(app, route, singular, plural);
    }
}
