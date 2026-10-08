using TimeEntry.ApiService.Security;

namespace TimeEntry.ApiService.Apis;

using static Microsoft.AspNetCore.Http.TypedResults;

public class E_RequestExpenseDetailApi<T> : BaseApi<T> where T : class
{
    private const string apiSubDir = "/expenseDetails";

    public override void Register(IEndpointRouteBuilder app)
    {
        string singular = "ExpenseDetail";
        string plural = singular + "s";

        // Get all
        app.MapGet(apiSubDir, GetAll)
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

    private static async Task<IResult> GetAll([FromServices] TimeEntryContext context, ClaimsPrincipal user)
    {
        var scope = await EmployeeScope.ForAsync(context, user);
        int[] ids = scope.EmployeeIds;
        E_RequestExpenseDetailRepo repo = new(context);
        // a detail belongs to whoever owns its sheet
        var rows = await repo.GetAllOrderByDescending(c => c.ExpenseDate,
            scope.All ? null : d => context.E_RequestExpenseSheet.Any(s => s.RequestExpenseSheetId == d.E_RequestExpenseSheetId && ids.Contains(s.EmployeeId)));
        return Ok(rows);
    }

    private static async Task<IResult> GetById([FromServices] TimeEntryContext context, ClaimsPrincipal user, int id)
    {
        var scope = await EmployeeScope.ForAsync(context, user);
        var owner = await RowOwners.ExpenseDetail(context, id);
        if (scope.Deny(owner?.EmployeeId) is { } denied)
            return denied;

        E_RequestExpenseDetailRepo repo = new(context);
        var row = await repo.GetByIdAsync(id);
        return row != null ? Results.Ok(row) : Results.NotFound();
    }

    private static async Task<IResult> CreateRow([FromServices] TimeEntryContext context, ClaimsPrincipal user, [FromBody] E_RequestExpenseDetail newRow)
    {
        // a detail may only be added to an expense sheet the caller may use
        var scope = await EmployeeScope.ForAsync(context, user);
        int? sheetOwner = await RowOwners.ExpenseSheet(context, newRow.E_RequestExpenseSheetId);
        if (sheetOwner == null)
            return Results.BadRequest(); // 400 error if the expense sheet does not exist
        if (scope.Deny(sheetOwner) is { } denied)
            return denied;

        E_RequestExpenseDetailRepo repo = new(context);
        await repo.AddAsync(newRow);
        return Results.Created($"/api/expenseDetails/{newRow.RequestExpenseDetailId}", newRow);
    }

    private static async Task<IResult> UpdateRow([FromServices] TimeEntryContext context, ClaimsPrincipal user, int id, [FromBody] E_RequestExpenseDetail updatedRow)
    {
        if (updatedRow.RequestExpenseDetailId != id)
            return Results.BadRequest(); // 400 error if the id in the URL and the id in the body disagree

        var scope = await EmployeeScope.ForAsync(context, user);
        var owner = await RowOwners.ExpenseDetail(context, id);
        if (scope.Deny(owner?.EmployeeId) is { } denied)
            return denied; // 404 if there is no row with that id, 403 if it is not the caller's
        updatedRow.E_RequestExpenseSheetId = owner!.ParentId; // the detail cannot be moved to another sheet

        E_RequestExpenseDetailRepo repo = new(context);
        var postUpdate = await repo.UpdateAsync(id, updatedRow);
        return Results.Ok(postUpdate);
    }

    private static async Task<IResult> DeleteRow([FromServices] TimeEntryContext context, ClaimsPrincipal user, int id)
    {
        var scope = await EmployeeScope.ForAsync(context, user);
        var owner = await RowOwners.ExpenseDetail(context, id);
        if (scope.Deny(owner?.EmployeeId) is { } denied)
            return denied;

        E_RequestExpenseDetailRepo repo = new(context);
        var successNum = await repo.DeleteAsync("E_RequestExpenseDetail", id);
        if (successNum == 0)
            return Results.Ok();
        else if (successNum == -1)
            return Results.NotFound(); // cannot delete because does not exist
        else
            return Results.BadRequest(); // cannot delete because "in use"
    }
}
