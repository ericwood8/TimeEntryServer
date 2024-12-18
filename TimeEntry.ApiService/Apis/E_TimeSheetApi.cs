namespace TimeEntry.ApiService.Apis;

using static Microsoft.AspNetCore.Http.TypedResults;

public class E_TimeSheetApi<T> : BaseApi<T> where T : class
{
    public override void Register(WebApplication app)
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
        .ProducesProblem(500);

        // Create new 
        app.MapPost(_apiSubDir, CreateRow)
        .WithName($"Create{singular}")
        .WithOpenApi()
        .ProducesProblem(422)
        .ProducesProblem(500);

        // Update existing 
        app.MapPut(_apiSubDir + "/{id:int}", UpdateRow)
        .WithName($"Update{singular}")
        .WithOpenApi()
        .ProducesProblem(404)
        .ProducesProblem(500);

        // Delete 
        app.MapDelete(_apiSubDir + "/{id:int}", DeleteRow)
        .WithName($"Delete{singular}")
        .WithOpenApi()
        .ProducesProblem(404)
        .ProducesProblem(500);
    }

    private static async Task<IResult> GetAll([FromServices] TimeEntryContext context)
    {
        var results = await GetContext(context)
          .OrderBy(c => c.WhenEntered)
          .ToListAsync();

        return Ok(results);
    }

    private static async Task<IResult> GetById([FromServices] TimeEntryContext context, int id)
    {
        var row = await GetContext(context).FindAsync(id);
        return row != null ? Results.Ok(row) : Results.NotFound();
    }

    private static async Task<IResult> CreateRow([FromServices] TimeEntryContext context, [FromBody] E_TimeSheet newRow)
    {
        GetContext(context).Add(newRow);
        await context.SaveChangesAsync();
        return Results.Created($"/api/timesheets/{newRow.TimeSheetId}", newRow);
    }

    private static async Task<IResult> UpdateRow([FromServices] TimeEntryContext context, int id, [FromBody] E_TimeSheet updatedRow)
    {
        var rowToUpdate = await GetContext(context).FindAsync(id);
        if (rowToUpdate == null) return Results.NotFound();

        rowToUpdate.WhenEntered = updatedRow.WhenEntered;
        rowToUpdate.EmployeeId = updatedRow.EmployeeId;
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

    private static DbSet<E_TimeSheet> GetContext(TimeEntryContext context)
    {
        return context.E_TimeSheet;
    }
}