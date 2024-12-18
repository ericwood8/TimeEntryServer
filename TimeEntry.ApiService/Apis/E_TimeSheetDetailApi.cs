namespace TimeEntry.ApiService.Apis;

using static Microsoft.AspNetCore.Http.TypedResults;

public class E_TimeSheetDetailApi<T> : BaseApi<T> where T : class
{
    public override void Register(WebApplication app)
    {
        BreakIntoStrings(out string singular, out string plural, out string apiSubDir);

        // Get all of timesheet
        app.MapGet("/timesheetDetails/timeSheet/{timesheetId:int}", GetAllOfTimesheet)
       .WithName("GetTimeSheetDetails")
       .WithOpenApi()
       .Produces<IEnumerable<T>>()
       .ProducesProblem(404)
       .ProducesProblem(500);

        // Get by ID
        app.MapGet("/timesheetDetails/{id:int}", GetById)
        .WithName("GetTimeSheetDetailById")
        .WithOpenApi()
        .Produces<T>()
        .ProducesProblem(500);

        // Create new 
        app.MapPost("/timesheetDetails", CreateRow)
        .WithName("CreateTimeSheetDetail")
        .WithOpenApi()
        .ProducesProblem(422)
        .ProducesProblem(500);

        // Update existing 
        app.MapPut("/timesheetDetails/{id:int}", UpdateRow)
        .WithName("UpdateTimeSheetDetails")
        .WithOpenApi()
        .ProducesProblem(404)
        .ProducesProblem(500);

        // Delete 
        app.MapDelete("/timesheetDetails/{id:int}", DeleteRow)
        .WithName("DeleteTimeSheetDetail")
        .WithOpenApi()
        .ProducesProblem(404)
        .ProducesProblem(500);
    }

    private static async Task<IResult> GetAllOfTimesheet([FromServices] TimeEntryContext context, int timesheetId)
    {
        var results = await GetContext(context)
          .Where(t => t.E_TimeSheetId.Equals(timesheetId))
          .OrderBy(c => c.ProjectId)
          .ToListAsync();

        return Ok(results);
    }

    private static async Task<IResult> GetById([FromServices] TimeEntryContext context, int id)
    {
        var row = await GetContext(context).FindAsync(id);
        return row != null ? Results.Ok(row) : Results.NotFound();
    }

    private static async Task<IResult> CreateRow([FromServices] TimeEntryContext context, [FromBody] E_TimeSheetDetail newRow)
    {
        GetContext(context).Add(newRow);
        await context.SaveChangesAsync();
        return Results.Created($"/api/timesheetDetails/{newRow.TimeSheetDetailId}", newRow);
    }

    private static async Task<IResult> UpdateRow([FromServices] TimeEntryContext context, int id, [FromBody] E_TimeSheetDetail updatedRow)
    {
        var rowToUpdate = await GetContext(context).FindAsync(id);
        if (rowToUpdate == null) return Results.NotFound();
                
        rowToUpdate.ProjectId = updatedRow.ProjectId;
        rowToUpdate.ProjectTaskId = updatedRow.ProjectTaskId;
        rowToUpdate.SundayHours = updatedRow.SundayHours;
        rowToUpdate.MondayHours = updatedRow.MondayHours;
        rowToUpdate.TuesdayHours = updatedRow.TuesdayHours;
        rowToUpdate.WednesdayHours = updatedRow.WednesdayHours;
        rowToUpdate.ThursdayHours = updatedRow.ThursdayHours;
        rowToUpdate.FridayHours = updatedRow.FridayHours;
        rowToUpdate.SaturdayHours = updatedRow.SaturdayHours;
        rowToUpdate.Notes = updatedRow.Notes;

        await context.SaveChangesAsync();
        return Results.Ok(rowToUpdate);
    }

    private static async Task<IResult> DeleteRow([FromServices] TimeEntryContext context, int id)
    {
        var rowToDelete = await GetContext(context).FindAsync(id);
        if (rowToDelete == null) return Results.NotFound();

        GetContext(context).Remove(rowToDelete);
        await context.SaveChangesAsync();
        return Results.NoContent();
    }

    private static DbSet<E_TimeSheetDetail> GetContext(TimeEntryContext context)
    {
        return context.E_TimeSheetDetail;
    }
}