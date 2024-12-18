namespace TimeEntry.ApiService.Apis;

using static Microsoft.AspNetCore.Http.TypedResults;

public class ResponseApi<T> : BaseApi<T> where T : class
{
    private const string apiSubDir = "/responses";

    public override void Register(WebApplication app)
    {
        BreakIntoStrings(out string singular, out string plural, out string apiSubDir);

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
        .ProducesProblem(500);

        // Create new 
        app.MapPost(apiSubDir, CreateRow)
        .WithName($"Create{singular}")
        .WithOpenApi()
        .ProducesProblem(500);

        // Update existing 
        app.MapPut(apiSubDir + "/{id:int}", UpdateRow)
        .WithName($"Update{singular}")
        .WithOpenApi()
        .ProducesProblem(404)
        .ProducesProblem(500);

        // Delete 
        app.MapDelete(apiSubDir + "/{id:int}", DeleteRow)
        .WithName($"Delete{singular}")
        .WithOpenApi()
        .ProducesProblem(404)
        .ProducesProblem(500);
    }

    private static async Task<IResult> GetAll([FromServices] TimeEntryContext context)
    {
        var results = await GetContext(context)
          .OrderByDescending(c => c.WhenResponded)
          .ToListAsync();

        return Ok(results);
    }

    private static async Task<IResult> GetById([FromServices] TimeEntryContext context, int id)
    {
        var row = await GetContext(context).FindAsync(id);
        return row != null ? Results.Ok(row) : Results.NotFound();
    }

    private static async Task<IResult> CreateRow([FromServices] TimeEntryContext context, [FromBody] TimeEntry.Common.Data.Entities.Response newRow)
    {
        GetContext(context).Add(newRow);
        await context.SaveChangesAsync();
        return Results.Created($"/api{apiSubDir}/{newRow.ResponseId}", newRow);
    }

    private static async Task<IResult> UpdateRow([FromServices] TimeEntryContext context, int id, [FromBody] TimeEntry.Common.Data.Entities.Response updatedRow)
    {
        var rowToUpdate = await GetContext(context).FindAsync(id);
        if (rowToUpdate == null) return Results.NotFound();

        rowToUpdate.ManagerId = updatedRow.ManagerId;
        rowToUpdate.E_RequestId = updatedRow.E_RequestId;
        rowToUpdate.ResponseTypeId = updatedRow.ResponseTypeId;
        rowToUpdate.WhenResponded = updatedRow.WhenResponded;

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

    private static DbSet<Response> GetContext(TimeEntryContext context)
    {
        return context.Response;
    }
}
