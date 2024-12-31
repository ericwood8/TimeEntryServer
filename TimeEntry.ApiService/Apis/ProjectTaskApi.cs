namespace TimeEntry.ApiService.Apis;

using static Microsoft.AspNetCore.Http.TypedResults;

public class ProjectTaskApi<T> : BaseApi<T> where T : BaseNameActiveEntity
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

        // special - Get all of Task 
        app.MapGet(apiSubDir + "/project/{id}", GetAllOfProject)
        .WithName($"Get{plural}OfProject")
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
        ProjectTaskRepo repo = new(context);
        var rows = await repo.GetAll();
        return Ok(rows);
    }

    private static async Task<IResult> GetById([FromServices] TimeEntryContext context, int id)
    {
        ProjectTaskRepo repo = new(context);
        var row = await repo.GetByIdAsync(id);
        return row != null ? Results.Ok(row) : Results.NotFound();
    }

    private static async Task<IResult> GetAllOfProject([FromServices] TimeEntryContext context, int id)
    {
        ProjectTaskRepo repo = new(context);
        var rows = await repo.GetAllOfProject(id);
        return Ok(rows);
    }

    private static async Task<IResult> GetByName([FromServices] TimeEntryContext context, string name)
    {
        if (name.IsNameBad())
            return Results.BadRequest(); // 400 error if bad characters or empty

        NameActiveRepo<ProjectTask> repo = new(context);
        var rows = await repo.GetByName(name);
        return rows != null ? Results.Ok(rows) : Results.NotFound();
    }

    private static async Task<IResult> CreateRow([FromServices] TimeEntryContext context, [FromBody] ProjectTask newRow)
    {
        newRow.Name = newRow.Name.Trim();
        if (newRow.Name.IsNameBad())
            return Results.BadRequest();  // 400 error if bad characters or empty

        ProjectTaskRepo repo = new(context);
        bool success = await repo.AddAsync(newRow);
        if (success)
            return Results.Created($"/api/projectTasks/{newRow.ProjectTaskId}", newRow);
        else
            return Results.UnprocessableEntity(); // 422 error if Duplicate Name           
    }

    private static async Task<IResult> UpdateRow([FromServices] TimeEntryContext context, int id, [FromBody] ProjectTask updatedRow)
    {
        if (updatedRow == null)
            return Results.NotFound();

        updatedRow.Name = updatedRow.Name.Trim();
        if (updatedRow.Name.IsNameBad())
            return Results.BadRequest(); // 400 error if bad characters or empty

        //if (taskRepo.IsDupOnUpdate(id, updatedRow.Name))
        //    return Results.UnprocessableEntity(); // 422 error if Duplicate Name 

        ProjectTaskRepo repo = new(context);
        var postUpdate = await repo.UpdateAsync(id, updatedRow);

        return Results.Ok(postUpdate);
    }

    private static async Task<IResult> DeleteRow([FromServices] TimeEntryContext context, int id)
    {
        ProjectTaskRepo repo = new(context);
        var successNum = await repo.DeleteAsync("ProjectTask", id);
        if (successNum == 0)
            return Results.Ok();
        else if (successNum == -1)
            return Results.NotFound(); // cannot delete because does not exist
        else
            return Results.BadRequest(); // cannot delete because "in use"
    }
}