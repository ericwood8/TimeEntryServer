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
        GenericRepo<E_TimeSheetDetail> repo = new(context);
        var row = await repo.GetByIdAsync(id);
        return row != null ? Results.Ok(row) : Results.NotFound();
    }

    private static async Task<IResult> CreateRow([FromServices] TimeEntryContext context, [FromBody] E_TimeSheetDetail newRow)
    {
        GenericRepo<E_TimeSheetDetail> repo = new(context);
        bool success = await repo.AddAsync(newRow);
        if (success)
            return Results.Created($"/api/timesheetDetails/{newRow.TimeSheetDetailId}", newRow);
        else
            return Results.NoContent();        
    }

    private static async Task<IResult> UpdateRow([FromServices] TimeEntryContext context, int id, [FromBody] E_TimeSheetDetail updatedRow)
    {
        GenericRepo<E_TimeSheetDetail> repo = new(context);
        var postUpdate = await repo.UpdateAsync(id, updatedRow);
        return Results.Ok(postUpdate);
    }

    private static async Task<IResult> DeleteRow([FromServices] TimeEntryContext context, int id)
    {
        GenericRepo<E_TimeSheetDetail> repo = new(context);
        var successNum = await repo.DeleteAsync("E_TimeSheetDetail", id);
        if (successNum == 0)
            return Results.Ok();
        else if (successNum == -1)
            return Results.NotFound(); // cannot delete because does not exist
        else
            return Results.BadRequest(); // cannot delete because "in use"
    }

    private static DbSet<E_TimeSheetDetail> GetContext(TimeEntryContext context)
    {
        return context.E_TimeSheetDetail;
    }
}