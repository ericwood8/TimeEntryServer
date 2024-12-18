namespace TimeEntry.ApiService.Apis;

using static Microsoft.AspNetCore.Http.TypedResults;

public class EmployeeApi<T> : BaseApi<T> where T : class
{
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
        .ProducesProblem(400)
        .ProducesProblem(422)
        .ProducesProblem(500);

        // Update existing 
        app.MapPut(apiSubDir + "/{id:int}", UpdateRow)
        .WithName($"Update{singular}")
        .WithOpenApi()
        .ProducesProblem(400)
        .ProducesProblem(404)
        .ProducesProblem(422)
        .ProducesProblem(500);

        // Delete 
        app.MapDelete(apiSubDir + "/{id:int}", DeleteRow)
        .WithName($"Delete{singular}")
        .WithOpenApi()
        .ProducesProblem(404)
        .ProducesProblem(500);

        // Get by Name
        app.MapGet(apiSubDir + "/{name}", GetByName)
        .WithName($"Get{singular}ByName")
        .WithOpenApi()
        .Produces<T>()
        .ProducesProblem(400)
        .ProducesProblem(404)
        .ProducesProblem(500);
    }

    private static async Task<IResult> GetAll([FromServices] TimeEntryContext context)
    {
        var results = await GetContext(context)
            .Where(e => e.IsActive) // only fetch active
            .Include(e => e.Manager)
            .Include(e => e.Department)
            .Include(e => e.DepartmentTeam)
            .OrderBy(e => e.Name)
            .ToListAsync();

        return Ok(results);
    }

    private static async Task<IResult> GetById([FromServices] TimeEntryContext context, int id)
    {
        var row = await GetContext(context).FindAsync(id);
        return row != null ? Results.Ok(row) : Results.NotFound();
    }

    private static async Task<IResult> GetByName([FromServices] TimeEntryContext context, string name)
    {
        if (name.IsNameBad())
            return Results.BadRequest(); // 400 error if bad characters or empty

        var rows = await GetContext(context)
            .Where(d => d.Name.StartsWith(name) && d.IsActive)
            .ToListAsync();
        return rows != null ? Results.Ok(rows) : Results.NotFound();
    }
    private static async Task<IResult> CreateRow([FromServices] TimeEntryContext context, [FromBody] Employee newRow)
    {
        newRow.Name = newRow.Name.Trim();
        if (newRow.Name.IsNameBad())
            return Results.BadRequest();  // 400 error if bad characters or empty
        else if (IsDup(context, newRow.Name))
            return Results.UnprocessableEntity(); // 422 error if Duplicate Name

        GetContext(context).Add(newRow);
        await context.SaveChangesAsync();
        return Results.Created($"/api/employees/{newRow.EmployeeId}", newRow);
    }

    private static async Task<IResult> UpdateRow([FromServices] TimeEntryContext context, int id, [FromBody] Employee updatedRow)
    {
        if (updatedRow.Name.IsNameBad())
            return Results.BadRequest(); // 400 error if bad characters or empty
        else if (IsDup(context, updatedRow.Name))
            return Results.UnprocessableEntity(); // 422 error if Duplicate Name

        var rowToUpdate = await GetContext(context).FindAsync(id);
        if (rowToUpdate == null) return Results.NotFound();

        rowToUpdate.Name = updatedRow.Name;
        rowToUpdate.ManagerId = updatedRow.ManagerId;
        rowToUpdate.DepartmentId = updatedRow.DepartmentId;
        rowToUpdate.DepartmentTeamId = updatedRow.DepartmentTeamId;
        rowToUpdate.AvailableLeaveHours = updatedRow.AvailableLeaveHours;
        rowToUpdate.DonatedHrsReceived = updatedRow.DonatedHrsReceived;
        rowToUpdate.IsActive = updatedRow.IsActive;
        rowToUpdate.WhenLeft = updatedRow.WhenLeft;

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

    private static DbSet<Employee> GetContext(TimeEntryContext context)
    {
        return context.Employee;
    }

    private static bool IsDup(TimeEntryContext context, string newName)
    {
        var unqiueRow = GetContext(context).FirstOrDefault(d => d.Name.Equals(newName.Trim()) && d.IsActive);
        return (unqiueRow != null);  // already exists 
    }
}
