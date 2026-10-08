using TimeEntry.ApiService.Security;

namespace TimeEntry.ApiService.Apis;

using static Microsoft.AspNetCore.Http.TypedResults;

public class E_TimeSheetApi<T> : BaseApi<T> where T : class
{
    public override void Register(IEndpointRouteBuilder app)
    {
        BreakIntoStrings(out string singular, out string plural, out string _apiSubDir);

        // Get all
        app.MapGet(_apiSubDir, GetAll)
       .WithName($"Get{plural}")
       .WithOpenApi()
       .Produces<IEnumerable<T>>()
       .ProducesProblem(404)
       .ProducesProblem(500);

        // Get by ID
        app.MapGet(_apiSubDir + "/{id:int}", GetById)
        .WithName($"Get{singular}ById")
        .WithOpenApi()
        .Produces<T>()
        .ProducesProblem(404)
        .ProducesProblem(500);

        // Create new
        app.MapPost(_apiSubDir, CreateRow)
        .WithName($"Create{singular}")
        .WithOpenApi()
        .ProducesProblem(500);

        // Update existing
        app.MapPut(_apiSubDir + "/{id:int}", UpdateRow)
        .WithName($"Update{singular}")
        .WithOpenApi()
        .ProducesProblem(400)
        .ProducesProblem(404)
        .ProducesProblem(500);

        // Delete
        app.MapDelete(_apiSubDir + "/{id:int}", DeleteRow)
        .WithName($"Delete{singular}")
        .WithOpenApi()
        .ProducesProblem(404)
        .ProducesProblem(500);
    }

    private static async Task<IResult> GetAll([FromServices] TimeEntryContext context, ClaimsPrincipal user)
    {
        var scope = await EmployeeScope.ForAsync(context, user);
        int[] ids = scope.EmployeeIds;
        E_TimeSheetRepo repo = new(context);
        var rows = await repo.GetAllOrderByDescending(c => c.WhenEntered, scope.All ? null : t => ids.Contains(t.EmployeeId));
        return Ok(rows);
    }

    private static async Task<IResult> GetById([FromServices] TimeEntryContext context, ClaimsPrincipal user, int id)
    {
        var scope = await EmployeeScope.ForAsync(context, user);
        E_TimeSheetRepo repo = new(context);
        var row = await repo.GetByIdAsync(id);
        if (row == null)
            return Results.NotFound();
        return scope.Deny(row.EmployeeId) ?? Results.Ok(row);
    }

    private static async Task<IResult> CreateRow([FromServices] TimeEntryContext context, ClaimsPrincipal user, [FromBody] E_TimeSheet newRow)
    {
        // whose time sheet it is comes from the token; only Admin and Human Resources may enter one for somebody else
        if (!user.CanManageAll())
        {
            if (user.EmployeeId() == null)
                return Results.Forbid();
            newRow.EmployeeId = user.EmployeeId()!.Value;
        }

        E_TimeSheetRepo repo = new(context);
        await repo.AddAsync(newRow);
        return Results.Created($"/api{_apiSubDir}/{newRow.TimeSheetId}", newRow);
    }

    private static async Task<IResult> UpdateRow([FromServices] TimeEntryContext context, ClaimsPrincipal user, int id, [FromBody] E_TimeSheet updatedRow)
    {
        if (updatedRow.TimeSheetId != id)
            return Results.BadRequest(); // 400 error if the id in the URL and the id in the body disagree

        var scope = await EmployeeScope.ForAsync(context, user);
        int? owner = await RowOwners.TimeSheet(context, id);
        if (scope.Deny(owner) is { } denied)
            return denied; // 404 if there is no row with that id, 403 if it is not the caller's
        if (!user.CanManageAll())
            updatedRow.EmployeeId = owner!.Value; // the row cannot be handed to someone else

        E_TimeSheetRepo repo = new(context);
        var postUpdate = await repo.UpdateAsync(id, updatedRow);
        return Results.Ok(postUpdate);
    }

    private static async Task<IResult> DeleteRow([FromServices] TimeEntryContext context, ClaimsPrincipal user, int id)
    {
        var scope = await EmployeeScope.ForAsync(context, user);
        if (scope.Deny(await RowOwners.TimeSheet(context, id)) is { } denied)
            return denied;

        E_TimeSheetRepo repo = new(context);
        var successNum = await repo.DeleteAsync("E_TimeSheet", id);
        if (successNum == 0)
            return Results.Ok();
        else if (successNum == -1)
            return Results.NotFound(); // cannot delete because does not exist
        else
            return Results.BadRequest(); // cannot delete because "in use"
    }
}
