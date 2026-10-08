namespace TimeEntry.ApiService.Apis;

using Microsoft.IdentityModel.Tokens;
using static Microsoft.AspNetCore.Http.TypedResults;

public class DepartmentApi<T> : BaseApi<T> where T : BaseNameActiveEntity
{
    public DepartmentApi()
    {

    }

    public override void Register(IEndpointRouteBuilder app)
    {
        BreakIntoStrings(out string singular, out string plural, out string _apiSubDir);

        // Get all
        app.MapGet(_apiSubDir, GetAllIncludeTeams)
       .WithName($"Get{plural}")
       .Produces<IEnumerable<T>>()
       .ProducesProblem(404)
       .ProducesProblem(500);

        // Get by ID
        app.MapGet(_apiSubDir + "/{id:int}", GetById)
        .WithName($"Get{singular}ById")
        .Produces<T>()
        .ProducesProblem(404)
        .ProducesProblem(500);

        // Create new 
        app.MapPost(_apiSubDir, CreateRow)
        .WithName($"Create{singular}")
        .ProducesProblem(400)
        .ProducesProblem(422)
        .ProducesProblem(500);

        // Update existing 
        app.MapPut(_apiSubDir + "/{id:int}", UpdateRow)
        .WithName($"Update{singular}")
        .ProducesProblem(400)
        .ProducesProblem(404)
        .ProducesProblem(422)
        .ProducesProblem(500);

        // Delete 
        app.MapDelete(_apiSubDir + "/{id:int}", DeleteRow)
        .WithName($"Delete{singular}")
        .ProducesProblem(404)
        .ProducesProblem(500);

        // Get by Name
        app.MapGet(_apiSubDir + "/{name}", GetByName)
        .WithName($"Get{singular}ByName")
        .Produces<List<T>>()
        .ProducesValidationProblem(400)
        .ProducesProblem(500);
    }

    private static async Task<IResult> GetAllIncludeTeams([FromServices] TimeEntryContext context)
    {
        DepartmentRepo repo = new(context);
        var rows = await repo.GetAllIncludeTeams();
        return Ok(rows);
    }

    //private static async Task<Results<Ok<PaginatedItems<Department>>, BadRequest<string>>> GetPage([FromServices] TimeEntryContext context, [AsParameters] PaginationRequest paginationRequest)
    //{        
    //    var pageIndex = paginationRequest.PageIndex;
    //    DepartmentRepo repo = new(context); 
    //    var totalItems = repo.CountAsync();
    //    var itemsOnPage = await repo.GetPage(pageSize, pageIndex)
    //        .OrderBy(c => c.Name)
    //        .Skip(_pageSize * pageIndex)
    //        .Take(_pageSize)
    //        .ToListAsync();

    //    return Ok(new PaginatedItems<Department>(pageIndex, totalItems, itemsOnPage));
    //}

    private static async Task<IResult> GetById([FromServices] TimeEntryContext context, int id)
    {
        DepartmentRepo repo = new(context);
        var row = await repo.GetByIdAsync(id);
        return row != null ? Results.Ok(row) : Results.NotFound();
    }

    private static async Task<IResult> GetByName([FromServices] TimeEntryContext context, string name)
    {
        if (name.IsNameBad())
            return ApiProblems.BadName(); // 400 error if bad characters or empty

        DepartmentRepo repo = new(context);
        var rows = await repo.GetByName(name);
        return Results.Ok(rows); // no match is an empty list, not a 404
    }

    private static async Task<IResult> CreateRow([FromServices] TimeEntryContext context, [FromBody] Department newRow)
    {
        newRow.Name = newRow.Name.Trim();
        if (newRow.Name.IsNameBad())
            return ApiProblems.BadName();  // 400 error if bad characters or empty

        DepartmentRepo repo = new(context);
        bool success = await repo.AddAsync(newRow);
        if (success)
            return Results.Created($"/api{_apiSubDir}/{newRow.DepartmentId}", newRow);
        else 
            return ApiProblems.DuplicateName(); // 422 error if Duplicate Name
    }

    private static async Task<IResult> UpdateRow([FromServices] TimeEntryContext context, int id, [FromBody] Department updatedRow)
    {
        if (updatedRow == null) 
            return Results.NotFound();
        if (updatedRow.DepartmentId != id)
            return ApiProblems.IdMismatch(); // 400 error if the id in the URL and the id in the body disagree

        updatedRow.Name = updatedRow.Name.Trim();
        if (updatedRow.Name.IsNameBad())
            return ApiProblems.BadName(); // 400 error if bad characters or empty

        bool teamsSent = updatedRow.Teams != null; // no list at all means "leave the teams alone"; an empty list removes them all
        List<DepartmentTeam> incomingTeams = updatedRow.Teams ?? [];
        foreach (var team in incomingTeams)
        {
            team.Name = team.Name.Trim();
            if (team.Name.IsNameBad())
                return ApiProblems.BadName(); // 400 error if a team name has bad characters or is empty
        }

        DepartmentRepo repo = new(context);
        Department? stored = await repo.GetByIdIncludeTeams(id);
        if (stored == null)
            return ApiProblems.NotFound(); // 404 error if there is no row with that id
        if (updatedRow.IsActive && await repo.IsDupOnUpdateAsync(id, updatedRow.Name))
            return ApiProblems.DuplicateName(); // 422 error if Duplicate Name

        // a team that is sent with an id must already belong to this department
        Dictionary<int, DepartmentTeam> storedTeams = (stored.Teams ?? []).ToDictionary(t => t.DepartmentTeamId);
        if (incomingTeams.Any(t => t.DepartmentTeamId != 0 && !storedTeams.ContainsKey(t.DepartmentTeamId)))
            return ApiProblems.Invalid("A team in the list does not belong to this department.");

        // teams left out of the list are removed, unless something (an employee, a leave restriction) still uses them
        HashSet<int> keptIds = incomingTeams.Select(t => t.DepartmentTeamId).ToHashSet();
        List<DepartmentTeam> teamsToRemove = !teamsSent ? [] : storedTeams.Values.Where(t => t.IsActive && !keptIds.Contains(t.DepartmentTeamId)).ToList();
        foreach (var team in teamsToRemove)
        {
            if ((await context.SpCanDeleteAsync("DepartmentTeam", team.DepartmentTeamId)).Count > 0)
                return ApiProblems.Unprocessable("A team that employees or leave restrictions still use cannot be removed."); // 422 error if a removed team is in use
        }

        // one save: the department, the changed teams, the new teams and the removed teams succeed or fail together
        context.Entry(stored).CurrentValues.SetValues(updatedRow);
        foreach (var team in incomingTeams)
        {
            team.DepartmentId = id;
            if (team.DepartmentTeamId == 0)
                stored.Teams!.Add(team);
            else
                context.Entry(storedTeams[team.DepartmentTeamId]).CurrentValues.SetValues(team);
        }
        context.RemoveRange(teamsToRemove);
        await context.SaveChangesAsync();

        return Results.Ok(stored); // the saved department with its current teams
    }

    private static async Task<IResult> DeleteRow([FromServices] TimeEntryContext context, int id)
    {
        DepartmentRepo repo = new(context);
        var successNum = await repo.DeleteAsync("Department", id);
        if (successNum == 0)
            return Results.Ok();
        else if (successNum == -1)
            return ApiProblems.NotFound(); // cannot delete because does not exist
        else
            return ApiProblems.InUse(); // cannot delete because "in use"
    }
}