namespace TimeEntry.ApiService.Apis;

using static Microsoft.AspNetCore.Http.TypedResults;

public class ResponseApi<T> : BaseApi<T> where T : class
{
    public override void Register(IEndpointRouteBuilder app)
    {
        BreakIntoStrings(out string singular, out string plural, out string _apiSubDir);

        // Get all
        app.MapGet(_apiSubDir, GetAll)
       .WithName($"Get{plural}")
       .Produces<IEnumerable<ResponseDto>>()
       .Produces<PaginatedItems<ResponseDto>>()
       .ProducesProblem(400)
       .ProducesProblem(404)
       .ProducesProblem(500);

        // Get by ID
        app.MapGet(_apiSubDir + "/{id:int}", GetById)
        .WithName($"Get{singular}ById")
        .Produces<ResponseDto>()
        .ProducesProblem(404)
        .ProducesProblem(500);

        // Create new
        app.MapPost(_apiSubDir, CreateRow)
        .WithName($"Create{singular}")
        .ProducesProblem(500);

        // Update existing
        app.MapPut(_apiSubDir + "/{id:int}", UpdateRow)
        .WithName($"Update{singular}")
        .ProducesProblem(400)
        .ProducesProblem(404)
        .ProducesProblem(500);

        // Delete
        app.MapDelete(_apiSubDir + "/{id:int}", DeleteRow)
        .WithName($"Delete{singular}")
        .ProducesProblem(404)
        .ProducesProblem(500);
    }

    private static readonly SortMap<Response> sorts = new SortMap<Response>()
        .Add("whenResponded", r => r.WhenResponded).Add("responseId", r => r.ResponseId).Add("request", r => r.E_RequestId)
        .Add("manager", r => r.ManagerId).Add("responseType", r => r.ResponseTypeId);

    /// <summary> The whole list, or (with pageIndex / pageSize) one page of it; sort applies to both. See <see cref="ListQuery"/>. </summary>
    private static async Task<IResult> GetAll([FromServices] TimeEntryContext context, [AsParameters] ListQuery query) =>
        await context.Response.ToResultAsync(query, sorts, "whenResponded:desc", r => r.ResponseId, null, ResponseDto.From);

    private static async Task<IResult> GetById([FromServices] TimeEntryContext context, int id)
    {
        ResponseRepo repo = new(context);
        var row = await repo.GetByIdAsync(id);
        return row != null ? Results.Ok(ResponseDto.From(row)) : Results.NotFound();
    }

    private static async Task<IResult> CreateRow([FromServices] TimeEntryContext context, [FromBody] ResponseSave save)
    {
        Response newRow = save.ToEntity();
        ResponseRepo repo = new(context);
        await repo.AddAsync(newRow);
        return Results.Created($"/api{_apiSubDir}/{newRow.ResponseId}", ResponseDto.From(newRow));
    }

    private static async Task<IResult> UpdateRow([FromServices] TimeEntryContext context, int id, [FromBody] ResponseSave save)
    {
        Response updatedRow = save.ToEntity();
        if (updatedRow.ResponseId != id)
            return ApiProblems.IdMismatch(); // 400 error if the id in the URL and the id in the body disagree

        ResponseRepo repo = new(context);
        if (!await repo.ExistsAsync(id))
            return ApiProblems.NotFound(); // 404 error if there is no row with that id
        var postUpdate = await repo.UpdateAsync(id, updatedRow);
        return Results.Ok(ResponseDto.From(postUpdate));
    }

    private static async Task<IResult> DeleteRow([FromServices] TimeEntryContext context, int id)
    {
        ResponseRepo repo = new(context);
        var successNum = await repo.DeleteAsync("Response", id);
        if (successNum == 0)
            return Results.Ok();
        else if (successNum == -1)
            return ApiProblems.NotFound(); // cannot delete because does not exist
        else
            return ApiProblems.InUse(); // cannot delete because "in use"
    }
}
