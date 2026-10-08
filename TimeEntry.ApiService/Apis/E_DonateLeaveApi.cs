using TimeEntry.ApiService.Security;

namespace TimeEntry.ApiService.Apis;

using static Microsoft.AspNetCore.Http.TypedResults;

public class E_DonateLeaveApi<T> : BaseApi<T> where T : class
{
    public override void Register(IEndpointRouteBuilder app)
    {
        BreakIntoStrings(out string singular, out string plural, out string _apiSubDir);

        // Get all
        app.MapGet(_apiSubDir, GetAll)
       .WithName($"Get{plural}")
       .Produces<IEnumerable<T>>()
       .ProducesProblem(404)
       .ProducesProblem(500);

        // Get by ID
        app.MapGet(_apiSubDir + "/{id:int}", GetById)
        .WithName($"Get{singular}ById")
        .Produces<T>()
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

    private static async Task<IResult> GetAll([FromServices] TimeEntryContext context, ClaimsPrincipal user)
    {
        // a donation is visible to whoever gave and whoever received
        var scope = await EmployeeScope.ForAsync(context, user);
        int[] ids = scope.EmployeeIds;
        E_DonateLeaveRepo repo = new(context);
        var rows = await repo.GetAllOrderByDescending(c => c.WhenDonated,
            scope.All ? null : d => ids.Contains(d.DonateFrom_EmployeeId) || ids.Contains(d.DonateTo_EmployeeId));
        return Ok(rows);
    }

    private static async Task<IResult> GetById([FromServices] TimeEntryContext context, ClaimsPrincipal user, int id)
    {
        var scope = await EmployeeScope.ForAsync(context, user);
        E_DonateLeaveRepo repo = new(context);
        var row = await repo.GetByIdAsync(id);
        if (row == null)
            return Results.NotFound();
        return scope.Allows(row.DonateFrom_EmployeeId) || scope.Allows(row.DonateTo_EmployeeId) ? Results.Ok(row) : Results.Forbid();
    }

    private static async Task<IResult> CreateRow([FromServices] TimeEntryContext context, ClaimsPrincipal user, [FromBody] E_DonateLeave newRow)
    {
        // the hours leave the caller's balance: who gives comes from the token; only Admin and Human Resources may enter a donation for somebody else
        if (!user.CanManageAll())
        {
            if (user.EmployeeId() == null)
                return Results.Forbid();
            newRow.DonateFrom_EmployeeId = user.EmployeeId()!.Value;
        }

        E_DonateLeaveRepo repo = new(context);
        await repo.AddAsync(newRow);
        return Results.Created($"/api{_apiSubDir}/{newRow.DonateLeaveId}", newRow);
    }

    private static async Task<IResult> UpdateRow([FromServices] TimeEntryContext context, ClaimsPrincipal user, int id, [FromBody] E_DonateLeave updatedRow)
    {
        if (updatedRow.DonateLeaveId != id)
            return Results.BadRequest(); // 400 error if the id in the URL and the id in the body disagree

        var scope = await EmployeeScope.ForAsync(context, user);
        var stored = await RowOwners.Donation(context, id);
        if (scope.Deny(stored?.FromEmployeeId) is { } denied)
            return denied; // 404 if there is no row with that id, 403 if the caller is not the giver
        updatedRow.DonateFrom_EmployeeId = stored!.FromEmployeeId; // the giver cannot be changed

        E_DonateLeaveRepo repo = new(context);
        var postUpdate = await repo.UpdateAsync(id, updatedRow);
        return Results.Ok(postUpdate);
    }

    private static async Task<IResult> DeleteRow([FromServices] TimeEntryContext context, ClaimsPrincipal user, int id)
    {
        var scope = await EmployeeScope.ForAsync(context, user);
        if (scope.Deny((await RowOwners.Donation(context, id))?.FromEmployeeId) is { } denied)
            return denied;

        E_DonateLeaveRepo repo = new(context);
        var successNum = await repo.DeleteAsync("E_DonateLeave", id);
        if (successNum == 0)
            return Results.Ok();
        else if (successNum == -1)
            return Results.NotFound(); // cannot delete because does not exist
        else
            return Results.BadRequest(); // cannot delete because "in use"
    }
}
