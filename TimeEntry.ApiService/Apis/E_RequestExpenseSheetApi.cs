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
       .Produces<IEnumerable<ExpenseSheetDto>>()
       .Produces<PaginatedItems<ExpenseSheetDto>>()
       .ProducesProblem(400)
       .ProducesProblem(404)
       .ProducesProblem(500);

        // Get by ID
        app.MapGet(apiSubDir + "/{id:int}", GetById)
        .WithName($"Get{singular}ById")
        .Produces<ExpenseSheetDto>()
        .ProducesProblem(404)
        .ProducesProblem(500);

        // Create new 
        app.MapPost(apiSubDir, CreateRow)
        .WithName($"Create{singular}")
        .ProducesProblem(422)
        .ProducesProblem(500);

        // Update existing 
        app.MapPut(apiSubDir + "/{id:int}", UpdateRow)
        .WithName($"Update{singular}")
        .ProducesProblem(400)
        .ProducesProblem(404)
        .ProducesProblem(500);

        // Delete 
        app.MapDelete(apiSubDir + "/{id:int}", DeleteRow)
        .WithName($"Delete{singular}")
        .ProducesProblem(404)
        .ProducesProblem(500);
    }

    private static readonly SortMap<E_RequestExpenseSheet> sorts = new SortMap<E_RequestExpenseSheet>()
        .Add("requestExpenseSheetId", s => s.RequestExpenseSheetId).Add("employeeId", s => s.EmployeeId).Add("employee", s => s.Employee!.Name)
        .Add("projectId", s => s.ProjectId).Add("notes", s => s.Notes);

    /// <summary> The whole list with each sheet's details, or (with pageIndex / pageSize) one page of it; sort and search apply to both. See <see cref="ListQuery"/>. </summary>
    private static async Task<IResult> GetAllIncludingDetails([FromServices] TimeEntryContext context, ClaimsPrincipal user, [AsParameters] ListQuery query)
    {
        var scope = await EmployeeScope.ForAsync(context, user);
        int[] ids = scope.EmployeeIds;
        int[] named = await context.EmployeeIdsNamedAsync(query);
        IQueryable<E_RequestExpenseSheet> rows = scope.All ? context.E_RequestExpenseSheet : context.E_RequestExpenseSheet.Where(s => ids.Contains(s.EmployeeId));
        // the page is taken from the sheets; each sheet on it brings its details
        return await rows.ToResultAsync(query, sorts, "requestExpenseSheetId:desc", s => s.RequestExpenseSheetId,
            text => s => (s.Notes != null && s.Notes.Contains(text)) || EF.Constant(named).Contains(s.EmployeeId), ExpenseSheetDto.From, q => q.Include(s => s.ExpenseDetails));
    }

    private static async Task<IResult> GetById([FromServices] TimeEntryContext context, ClaimsPrincipal user, int id)
    {
        var scope = await EmployeeScope.ForAsync(context, user);
        E_RequestExpenseSheetRepo repo = new(context);
        var row = await repo.GetByIdAsync(id);
        if (row == null)
            return Results.NotFound();
        return scope.Deny(row.EmployeeId) ?? Results.Ok(ExpenseSheetDto.From(row));
    }

    private static async Task<IResult> CreateRow([FromServices] TimeEntryContext context, ClaimsPrincipal user, [FromBody] ExpenseSheetSave save)
    {
        E_RequestExpenseSheet newRow = save.ToEntity();
        // whose expenses they are comes from the token; only Admin and Human Resources may enter a sheet for somebody else
        if (!user.CanManageAll())
        {
            if (user.EmployeeId() == null)
                return Results.Forbid();
            newRow.EmployeeId = user.EmployeeId()!.Value;
        }

        E_RequestExpenseSheetRepo repo = new(context);
        await repo.AddAsync(newRow);
        return Results.Created($"/api{apiSubDir}/{newRow.RequestExpenseSheetId}", ExpenseSheetDto.From(newRow));
    }

    private static async Task<IResult> UpdateRow([FromServices] TimeEntryContext context, ClaimsPrincipal user, int id, [FromBody] ExpenseSheetSave save)
    {
        E_RequestExpenseSheet updatedRow = save.ToEntity();
        if (updatedRow.RequestExpenseSheetId != id)
            return ApiProblems.IdMismatch(); // 400 error if the id in the URL and the id in the body disagree

        var scope = await EmployeeScope.ForAsync(context, user);
        int? owner = await RowOwners.ExpenseSheet(context, id);
        if (scope.Deny(owner) is { } denied)
            return denied; // 404 if there is no row with that id, 403 if it is not the caller's
        updatedRow.EmployeeId = owner!.Value; // the sheet cannot be handed to someone else

        E_RequestExpenseSheetRepo repo = new(context);
        var postUpdate = await repo.UpdateAsync(id, updatedRow);
        return Results.Ok(ExpenseSheetDto.From(postUpdate));
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
            return ApiProblems.NotFound(); // cannot delete because does not exist
        else
            return ApiProblems.InUse(); // cannot delete because "in use"
    }
}
