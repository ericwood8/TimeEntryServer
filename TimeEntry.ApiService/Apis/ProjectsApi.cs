namespace TimeEntry.ApiService.Apis;
using static Microsoft.AspNetCore.Http.TypedResults;

public class ProjectsApi<T> : BaseApi<T> where T : class
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

    private static async Task<IResult> GetAll([FromServices]TimeEntryContext context)
    {
        var results = await GetContext(context)
            .Where(t => t.IsActive) // only fetch active
           .Include(p => p.Tasks)
          .OrderBy(c => c.Name)
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

    private static async Task<IResult> CreateRow([FromServices] TimeEntryContext context, [FromBody] Project newRow)
    {
        newRow.Name = newRow.Name.Trim();

        if (newRow.Name.IsNameBad())
            return Results.BadRequest();  // 400 error if bad characters or empty
        else if (IsDup(context, newRow.Name))  
            return Results.UnprocessableEntity(); // 422 error if Duplicate Name

        GetContext(context).Add(newRow);
        await context.SaveChangesAsync();
        return Results.Created($"/api/projects/{newRow.ProjectId}", newRow);
    }

    private static async Task<IResult> UpdateRow([FromServices] TimeEntryContext context, int id, [FromBody] Project updatedRow)
    {
        if (updatedRow.Name.IsNameBad())
            return Results.BadRequest(); // 400 error if bad characters or empty
        else if (IsDup(context, updatedRow.Name))
            return Results.UnprocessableEntity(); // 422 error if Duplicate Name

        var rowToUpdate = await GetContext(context).Include(c => c.Tasks).FirstOrDefaultAsync(c => c.ProjectId == id);
        if (rowToUpdate == null) return Results.NotFound();

        rowToUpdate.Name = updatedRow.Name;
        rowToUpdate.IsDefault = updatedRow.IsDefault;
        rowToUpdate.IsActive = updatedRow.IsActive;

        var tasksToDelete = rowToUpdate.Tasks?.Where(t => !updatedRow.Tasks!.Any(s => s.Name == t.Name));
        if(tasksToDelete!.Any()) context.ProjectTask.RemoveRange(tasksToDelete!);

        rowToUpdate.Tasks = updatedRow.Tasks;

        await context.SaveChangesAsync();
        return Results.Ok(rowToUpdate);
    }

    private static async Task<IResult> DeleteRow([FromServices] TimeEntryContext context, int id)
    {
        var rowToDelete = await GetContext(context).FindAsync(id);
        if (rowToDelete == null) return Results.NotFound();

        var canDelete = await context.SpCanDeleteAsync("Project", id);
        if (canDelete.Any()) return Results.BadRequest();

        context.Project.Remove(rowToDelete);
        await context.SaveChangesAsync();
        return Results.NoContent();
    }

    private static DbSet<Project> GetContext(TimeEntryContext context)
    {
        return context.Project;
    }

    private static bool IsDup(TimeEntryContext context, string newName)
    {
        var unqiueRow = GetContext(context).FirstOrDefault(d => d.Name.Equals(newName.Trim()) && d.IsActive);
        return (unqiueRow != null);  // already exists 
    }
}