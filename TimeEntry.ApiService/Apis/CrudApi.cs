namespace TimeEntry.ApiService.Apis;

/// <summary> What a handler works with: the context of this request, the signed-in user and the request's services. </summary>
public sealed record CrudCall(TimeEntryContext Context, ClaimsPrincipal User, IServiceProvider Services);

/// <summary> Reading a row, or changing or deleting it: some rows may be seen by more people than may change them. </summary>
public enum CrudAccess { Read, Write }

/// <summary>
/// The five endpoints every table has (spec item E04): get by id, create, update, delete, and a list. One place decides the answers: 201 with a Location,
/// 400 for an id that disagrees with the body, 404 for a missing row, 422 for a name another active row has, 400 for a row that is in use. A class says what is
/// its own (the list, the rules in <see cref="PrepareCreateAsync"/> and <see cref="PrepareUpdateAsync"/>, who may touch a row in <see cref="AuthorizeAsync"/>)
/// and overrides <see cref="CreateAsync"/> or <see cref="UpdateAsync"/> only when the whole step differs (a department's teams, a user's password).
/// </summary>
/// <typeparam name="TEntity"> The table's class. </typeparam>
/// <typeparam name="TInput"> What the caller sends (the entity itself, or a Save record that holds only the columns a caller may set). </typeparam>
/// <typeparam name="TOutput"> What the caller gets back (the entity itself, or a Dto). </typeparam>
public abstract class CrudApi<TEntity, TInput, TOutput> : BaseApi<TEntity>
    where TEntity : class
    where TInput : class
    where TOutput : class
{
    // ---------------------------------------------------------------- what a class says

    protected abstract CrudStore<TEntity> Store(TimeEntryContext context);

    protected abstract int KeyOf(TEntity row);

    protected abstract TEntity ToEntity(TInput input);

    /// <summary> The row as the caller gets it. A class that overrides both <see cref="ReadAsync"/> and <see cref="OutputAsync"/> does not need it. </summary>
    protected virtual TOutput ToOutput(TEntity row) => throw new NotSupportedException("This class builds its output in ReadAsync and OutputAsync.");

    /// <summary> The name the table is known by to the delete check (spCanDelete). </summary>
    protected virtual string Table => typeof(TEntity).Name;

    /// <summary> The names the route and the OpenAPI operations are made from. </summary>
    protected virtual void Names(out string singular, out string plural, out string route) => BreakIntoStrings(out singular, out plural, out route);

    /// <summary> False for a table whose rows are only listed under a parent (see <see cref="RegisterExtras"/>). </summary>
    protected virtual bool HasList => true;

    /// <summary> True when the list answers a page when asked (<see cref="ListQuery"/>). </summary>
    protected virtual bool PagedList => false;

    protected virtual Task<IResult> ListAsync(CrudCall call, ListQuery query) => throw new NotSupportedException("A class with a list overrides ListAsync.");

    // ---------------------------------------------------------------- hooks

    /// <summary> Null when the caller may read (or change) the row, else the answer to send; also 404 when there is no such row. Used by get by id and delete. </summary>
    protected virtual Task<IResult?> AuthorizeAsync(CrudCall call, int id, CrudAccess access) => Task.FromResult<IResult?>(null);

    /// <summary> Checks and completes a new row before it is saved (clean the name, set the owner from the token, refuse a bad combination). Null to go on. </summary>
    protected virtual Task<IResult?> PrepareCreateAsync(CrudCall call, TEntity row, TInput input) => Task.FromResult<IResult?>(null);

    /// <summary> Checks and completes a changed row before it is saved. The default only requires that the row exists. Null to go on. </summary>
    protected virtual async Task<IResult?> PrepareUpdateAsync(CrudCall call, int id, TEntity row, TInput input) =>
        await Store(call.Context).ExistsAsync(id) ? null : ApiProblems.NotFound();

    /// <summary> The row as the caller gets it after a read, when the plain mapping of the entity is not enough (a time sheet detail comes with the names). </summary>
    protected virtual async Task<TOutput?> ReadAsync(CrudCall call, int id)
    {
        TEntity? row = await Store(call.Context).GetByIdAsync(id);
        return row == null ? default : ToOutput(row);
    }

    /// <summary> The saved row as the caller gets it back from a create or an update. </summary>
    protected virtual Task<TOutput> OutputAsync(CrudCall call, TEntity saved) => Task.FromResult(ToOutput(saved));

    /// <summary> Routes of its own (a list under a parent, a lookup): called once from <see cref="Register"/>. </summary>
    protected virtual void RegisterExtras(IEndpointRouteBuilder app, string route, string singular, string plural)
    {
    }

    // ---------------------------------------------------------------- the five endpoints

    protected string Route { get; private set; } = "";

    protected virtual async Task<IResult> GetByIdAsync(CrudCall call, int id)
    {
        if (await AuthorizeAsync(call, id, CrudAccess.Read) is { } denied)
            return denied;
        TOutput? output = await ReadAsync(call, id);
        return output is null ? ApiProblems.NotFound() : TypedResults.Ok(output);
    }

    protected virtual async Task<IResult> CreateAsync(CrudCall call, TInput input)
    {
        TEntity row = ToEntity(input);
        if (await PrepareCreateAsync(call, row, input) is { } refused)
            return refused;
        if (!await Store(call.Context).AddAsync(row))
            return ApiProblems.DuplicateName(); // 422: another active row has that name
        return Results.Created($"/api{Route}/{KeyOf(row)}", await OutputAsync(call, row));
    }

    protected virtual async Task<IResult> UpdateAsync(CrudCall call, int id, TInput input)
    {
        TEntity row = ToEntity(input);
        if (KeyOf(row) != id)
            return ApiProblems.IdMismatch(); // 400: the id in the URL and the id in the body disagree
        if (await PrepareUpdateAsync(call, id, row, input) is { } refused)
            return refused;
        TEntity? saved = await Store(call.Context).UpdateAsync(id, row);
        return saved is null ? ApiProblems.DuplicateName() : TypedResults.Ok(await OutputAsync(call, saved)); // 422: another active row has that name
    }

    protected virtual async Task<IResult> DeleteAsync(CrudCall call, int id)
    {
        if (await AuthorizeAsync(call, id, CrudAccess.Write) is { } denied)
            return denied;
        int result = await Store(call.Context).DeleteAsync(Table, id);
        return result switch
        {
            0 => Results.Ok(),
            -1 => ApiProblems.NotFound(),   // there is no such row
            _ => ApiProblems.InUse()        // something still uses it
        };
    }

    protected static CrudCall Call(TimeEntryContext context, HttpContext http) => new(context, http.User, http.RequestServices);

    public override void Register(IEndpointRouteBuilder app)
    {
        Names(out string singular, out string plural, out string route);
        Route = route;

        if (HasList)
        {
            var list = app.MapGet(route, (TimeEntryContext context, HttpContext http, [AsParameters] ListQuery query) => ListAsync(Call(context, http), query))
                .WithName($"Get{plural}")
                .Produces<IEnumerable<TOutput>>();
            if (PagedList)
                list.Produces<PaginatedItems<TOutput>>().ProducesProblem(400);
            list.ProducesProblem(500);
        }

        app.MapGet(route + "/{id:int}", (TimeEntryContext context, HttpContext http, int id) => GetByIdAsync(Call(context, http), id))
            .WithName($"Get{singular}ById")
            .Produces<TOutput>()
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(500);

        app.MapPost(route, (TimeEntryContext context, HttpContext http, [FromBody] TInput input) => CreateAsync(Call(context, http), input))
            .WithName($"Create{singular}")
            .Produces<TOutput>(201)
            .ProducesProblem(400)
            .ProducesProblem(403)
            .ProducesProblem(422)
            .ProducesProblem(500);

        app.MapPut(route + "/{id:int}", (TimeEntryContext context, HttpContext http, int id, [FromBody] TInput input) => UpdateAsync(Call(context, http), id, input))
            .WithName($"Update{singular}")
            .Produces<TOutput>()
            .ProducesProblem(400)
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(422)
            .ProducesProblem(500);

        app.MapDelete(route + "/{id:int}", (TimeEntryContext context, HttpContext http, int id) => DeleteAsync(Call(context, http), id))
            .WithName($"Delete{singular}")
            .ProducesProblem(400)
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(500);

        RegisterExtras(app, route, singular, plural);
    }
}
