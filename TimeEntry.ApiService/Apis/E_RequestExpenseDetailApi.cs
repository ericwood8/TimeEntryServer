namespace TimeEntry.ApiService.Apis;

using static Microsoft.AspNetCore.Http.TypedResults;

public class E_RequestExpenseDetailApi<T> : BaseApi<T> where T : class
{
    private const string apiSubDir = "/expenseDetails";

    public override void Register(WebApplication app)
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

    private static async Task<IResult> GetAll([FromServices] TimeEntryContext context)
    {
        var results = await GetContext(context)
          .OrderBy(c => c.ExpenseDate)
          .ToListAsync();

        return Ok(results);
    }

    private static async Task<IResult> GetById([FromServices] TimeEntryContext context, int id)
    {
        var row = await GetContext(context).FindAsync(id);
        return row != null ? Results.Ok(row) : Results.NotFound();
    }

    private static async Task<IResult> CreateRow([FromServices] TimeEntryContext context, [FromBody] E_RequestExpenseDetail newRow)
    {
        GetContext(context).Add(newRow);
        await context.SaveChangesAsync();
        return Results.Created($"/api/expenseDetails/{newRow.RequestExpenseDetailId}", newRow);
    }

    private static async Task<IResult> UpdateRow([FromServices] TimeEntryContext context, int id, [FromBody] E_RequestExpenseDetail updatedRow)
    {
        var rowToUpdate = await GetContext(context).FindAsync(id);
        if (rowToUpdate == null) return Results.NotFound();

        rowToUpdate.ExpenseTypeId = updatedRow.ExpenseTypeId;
        rowToUpdate.E_RequestExpenseSheetId = updatedRow.E_RequestExpenseSheetId;
        rowToUpdate.ExpenseDate = updatedRow.ExpenseDate;
        rowToUpdate.ReimbursableAmount = updatedRow.ReimbursableAmount;
        rowToUpdate.ReceiptProvided = updatedRow.ReceiptProvided;
        rowToUpdate.AttachedReceiptFilePath = updatedRow.AttachedReceiptFilePath;
        rowToUpdate.VendorName = updatedRow.VendorName;
        rowToUpdate.Notes = updatedRow.Notes;
        rowToUpdate.LodgingNights = updatedRow.LodgingNights;
        rowToUpdate.MilesForPerDiem = updatedRow.MilesForPerDiem;
        rowToUpdate.ExcuseForNoReceipt = updatedRow.ExcuseForNoReceipt;
        rowToUpdate.FlightDeparture = updatedRow.FlightDeparture;
        rowToUpdate.FlightReturn = updatedRow.FlightReturn;

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

    private static DbSet<E_RequestExpenseDetail> GetContext(TimeEntryContext context)
    {
        return context.E_RequestExpenseDetail;
    }
}