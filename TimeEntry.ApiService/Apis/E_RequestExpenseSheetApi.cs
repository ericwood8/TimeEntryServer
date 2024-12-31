namespace TimeEntry.ApiService.Apis;

using static Microsoft.AspNetCore.Http.TypedResults;

public class E_RequestExpenseSheetApi<T> : BaseApi<T> where T : class
{
    private const string apiSubDir = "/expenseSheets";

    public override void Register(WebApplication app)
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
        .ProducesProblem(404)
        .ProducesProblem(500);

        // Delete 
        app.MapDelete(apiSubDir + "/{id:int}", DeleteRow)
        .WithName($"Delete{singular}")
        .WithOpenApi()
        .ProducesProblem(404)
        .ProducesProblem(500);
    }

    private static async Task<IResult> GetAllIncludingDetails([FromServices] TimeEntryContext context)
    {
        var results = await GetContext(context)
            .Include(s => s.ExpenseDetails)
            .ToListAsync();

        return Ok(results);
    }

    private static async Task<IResult> GetById([FromServices] TimeEntryContext context, int id)
    {
        GenericRepo<E_RequestExpenseSheet> repo = new(context);
        var row = await repo.GetByIdAsync(id);
        return row != null ? Results.Ok(row) : Results.NotFound();
    }

    private static async Task<IResult> CreateRow([FromServices] TimeEntryContext context, [FromBody] E_RequestExpenseSheet newRow)
    {
        GenericRepo<E_RequestExpenseSheet> repo = new(context);
        bool success = await repo.AddAsync(newRow);
        if (success)
            return Results.Created($"/api{apiSubDir}/{newRow.RequestExpenseSheetId}", newRow);
        else
            return Results.NoContent();
    }

    private static async Task<IResult> UpdateRow([FromServices] TimeEntryContext context, int id, [FromBody] E_RequestExpenseSheet updatedRow)
    {
        GenericRepo<E_RequestExpenseSheet> repo = new(context);
        var postUpdate = await repo.UpdateAsync(id, updatedRow);
        return Results.Ok(postUpdate);
    }

    private static async Task<IResult> DeleteRow([FromServices] TimeEntryContext context, int id)
    {
        GenericRepo<E_RequestExpenseSheet> repo = new(context);
        var successNum = await repo.DeleteAsync("E_RequestExpenseSheet", id);
        if (successNum == 0)
            return Results.Ok();
        else if (successNum == -1)
            return Results.NotFound(); // cannot delete because does not exist
        else
            return Results.BadRequest(); // cannot delete because "in use"
    }

    private static DbSet<E_RequestExpenseSheet> GetContext(TimeEntryContext context)
    {
        return context.E_RequestExpenseSheet;
    }
}