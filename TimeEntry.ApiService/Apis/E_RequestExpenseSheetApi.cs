using TimeEntry.ApiService.Security;

namespace TimeEntry.ApiService.Apis;

using static Microsoft.AspNetCore.Http.TypedResults;

public class E_RequestExpenseSheetApi<T> : BaseApi<T> where T : class
{
    private const string apiSubDir = "/expenseSheets";

    public override void Register(IEndpointRouteBuilder app)
    {
        string singular = "ExpenseSheet";
        string plural = singular + "s";

        // Get all
        app.MapGet(apiSubDir, GetAllIncludingDetails)
       .WithName($"Get{plural}")
       .WithOpenApi()
       .Produces<IEnumerable<T>>()
       .ProducesProblem(404)
       .ProducesProblem(500);

        // Get by ID
        app.MapGet(apiSubDir + "/{id:int}", GetById)
        .WithName($"Get{singular}ById")
        .WithOpenApi()
        .Produces<T>()
        .ProducesProblem(404)
        .ProducesProblem(500);

        // Create new 
        app.MapPost(apiSubDir, CreateRow)
        .WithName($"Create{singular}")
        .WithOpenApi()
        .ProducesProblem(422)
        .ProducesProblem(500);

        // Update existing 
        app.MapPut(apiSubDir + "/{id:int}", UpdateRow)
        .WithName($"Update{singular}")
        .WithOpenApi()
        .ProducesProblem(400)
        .ProducesProblem(404)
        .ProducesProblem(500);

        // Delete 
        app.MapDelete(apiSubDir + "/{id:int}", DeleteRow)
        .WithName($"Delete{singular}")
        .WithOpenApi()
        .ProducesProblem(404)
        .ProducesProblem(500);
    }

    private static async Task<IResult> GetAllIncludingDetails([FromServices] TimeEntryContext context, ClaimsPrincipal user)
    {
        var scope = await EmployeeScope.ForAsync(context, user);
        int[] ids = scope.EmployeeIds;
        E_RequestExpenseSheetRepo repo = new(context);
        var rows = await repo.GetAllIncludingDetails(scope.All ? null : s => ids.Contains(s.EmployeeId));
        return Ok(rows);
    }

    private static async Task<IResult> GetById([FromServices] TimeEntryContext context, ClaimsPrincipal user, int id)
    {
        var scope = await EmployeeScope.ForAsync(context, user);
        E_RequestExpenseSheetRepo repo = new(context);
        var row = await repo.GetByIdAsync(id);
        if (row == null)
            return Results.NotFound();
        return scope.Deny(row.EmployeeId) ?? Results.Ok(row);
    }

    private static async Task<IResult> CreateRow([FromServices] TimeEntryContext context, ClaimsPrincipal user, [FromBody] E_RequestExpenseSheet newRow)
    {
        // whose expenses they are comes from the token; only Admin and Human Resources may enter a sheet for somebody else
        if (!user.CanManageAll())
        {
            if (user.EmployeeId() == null)
                return Results.Forbid();
            newRow.EmployeeId = user.EmployeeId()!.Value;
        }

        E_RequestExpenseSheetRepo repo = new(context);
        await repo.AddAsync(newRow);
        return Results.Created($"/api{apiSubDir}/{newRow.RequestExpenseSheetId}", newRow);
    }

    private static async Task<IResult> UpdateRow([FromServices] TimeEntryContext context, ClaimsPrincipal user, int id, [FromBody] E_RequestExpenseSheet updatedRow)
    {
        if (updatedRow.RequestExpenseSheetId != id)
            return Results.BadRequest(); // 400 error if the id in the URL and the id in the body disagree

        var scope = await EmployeeScope.ForAsync(context, user);
        int? owner = await RowOwners.ExpenseSheet(context, id);
        if (scope.Deny(owner) is { } denied)
            return denied; // 404 if there is no row with that id, 403 if it is not the caller's
        updatedRow.EmployeeId = owner!.Value; // the sheet cannot be handed to someone else

        E_RequestExpenseSheetRepo repo = new(context);
        var postUpdate = await repo.UpdateAsync(id, updatedRow);
        return Results.Ok(postUpdate);
    }

    private static async Task<IResult> DeleteRow([FromServices] TimeEntryContext context, ClaimsPrincipal user, int id)
    {
        var scope = await EmployeeScope.ForAsync(context, user);
        if (scope.Deny(await RowOwners.ExpenseSheet(context, id)) is { } denied)
            return denied;

        E_RequestExpenseSheetRepo repo = new(context);
        var successNum = await repo.DeleteAsync("E_RequestExpenseSheet", id);
        if (successNum == 0)
            return Results.Ok();
        else if (successNum == -1)
            return Results.NotFound(); // cannot delete because does not exist
        else
            return Results.BadRequest(); // cannot delete because "in use"
    }
}
