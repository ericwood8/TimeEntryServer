using TimeEntry.ApiService.Security;
using TimeEntry.Common.Models;

namespace TimeEntry.ApiService.Apis;

using static Microsoft.AspNetCore.Http.TypedResults;

public class E_TimeSheetDetailApi<T> : BaseApi<T> where T : class
{
    public override void Register(IEndpointRouteBuilder app)
    {
        BreakIntoStrings(out string singular, out string plural, out string apiSubDir);

        // Get all of timesheet
        app.MapGet("/timesheetDetails/timeSheet/{timesheetId:int}", GetAllOfTimesheet)
       .WithName("GetTimeSheetDetails")
       .Produces<IEnumerable<TimeSheetDetailRow>>()
       .ProducesProblem(404)
       .ProducesProblem(500);

        // Get by ID
        app.MapGet("/timesheetDetails/{id:int}", GetById)
        .WithName("GetTimeSheetDetailById")
        .Produces<TimeSheetDetailRow>()
        .ProducesProblem(500);

        // Create new 
        app.MapPost("/timesheetDetails", CreateRow)
        .WithName("CreateTimeSheetDetail")
        .ProducesProblem(422)
        .ProducesProblem(500);

        // Update existing 
        app.MapPut("/timesheetDetails/{id:int}", UpdateRow)
        .WithName("UpdateTimeSheetDetails")
        .ProducesProblem(404)
        .ProducesProblem(500);

        // Delete 
        app.MapDelete("/timesheetDetails/{id:int}", DeleteRow)
        .WithName("DeleteTimeSheetDetail")
        .ProducesProblem(404)
        .ProducesProblem(500);
    }

    private static async Task<IResult> GetAllOfTimesheet([FromServices] TimeEntryContext context, ClaimsPrincipal user, int timesheetId)
    {
        var scope = await EmployeeScope.ForAsync(context, user);
        if (scope.Deny(await RowOwners.TimeSheet(context, timesheetId)) is { } denied)
            return denied;

        E_TimeSheetDetailRepo repo = new(context);
        var rows = await repo.GetAllOfTimesheetWithNames(timesheetId);
        return Results.Ok(rows);
    }

    private static async Task<IResult> GetById([FromServices] TimeEntryContext context, ClaimsPrincipal user, int id)
    {
        var scope = await EmployeeScope.ForAsync(context, user);
        var owner = await RowOwners.TimeSheetDetail(context, id);
        if (scope.Deny(owner?.EmployeeId) is { } denied)
            return denied;

        E_TimeSheetDetailRepo repo = new(context);
        var row = await repo.GetRowWithNames(id);
        return row != null ? Results.Ok(row) : Results.NotFound();
    }

    private static async Task<IResult> CreateRow([FromServices] TimeEntryContext context, ClaimsPrincipal user, [FromBody] TimeSheetDetailSave save)
    {
        E_TimeSheetDetail newRow = save.ToEntity();
        // a detail may only be added to a time sheet the caller may use
        var scope = await EmployeeScope.ForAsync(context, user);
        int? sheetOwner = await RowOwners.TimeSheet(context, newRow.E_TimeSheetId);
        if (sheetOwner == null)
            return ApiProblems.Invalid("There is no time sheet with that id."); // 400 error if the time sheet does not exist
        if (scope.Deny(sheetOwner) is { } denied)
            return denied;

        E_TimeSheetDetailRepo repo = new(context);
        await repo.AddAsync(newRow);
        return Results.Created($"/api/timesheetDetails/{newRow.TimeSheetDetailId}", await repo.GetRowWithNames(newRow.TimeSheetDetailId));
    }

    private static async Task<IResult> UpdateRow([FromServices] TimeEntryContext context, ClaimsPrincipal user, int id, [FromBody] TimeSheetDetailSave save)
    {
        E_TimeSheetDetail updatedRow = save.ToEntity();
        if (updatedRow.TimeSheetDetailId != id)
            return ApiProblems.IdMismatch(); // 400 error if the id in the URL and the id in the body disagree

        var scope = await EmployeeScope.ForAsync(context, user);
        var owner = await RowOwners.TimeSheetDetail(context, id);
        if (scope.Deny(owner?.EmployeeId) is { } denied)
            return denied; // 404 if there is no row with that id, 403 if it is not the caller's
        updatedRow.E_TimeSheetId = owner!.ParentId; // the detail cannot be moved to another time sheet

        E_TimeSheetDetailRepo repo = new(context);
        await repo.UpdateAsync(id, updatedRow);
        return Results.Ok(await repo.GetRowWithNames(id));
    }

    private static async Task<IResult> DeleteRow([FromServices] TimeEntryContext context, ClaimsPrincipal user, int id)
    {
        var scope = await EmployeeScope.ForAsync(context, user);
        var owner = await RowOwners.TimeSheetDetail(context, id);
        if (scope.Deny(owner?.EmployeeId) is { } denied)
            return denied;

        E_TimeSheetDetailRepo repo = new(context);
        var successNum = await repo.DeleteAsync("E_TimeSheetDetail", id);
        if (successNum == 0)
            return Results.Ok();
        else if (successNum == -1)
            return ApiProblems.NotFound(); // cannot delete because does not exist
        else
            return ApiProblems.InUse(); // cannot delete because "in use"
    }
}
