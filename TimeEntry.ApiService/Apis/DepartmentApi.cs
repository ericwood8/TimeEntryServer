namespace TimeEntry.ApiService.Apis;

/// <summary> Departments, listed with their teams. An update carries the teams too: see <see cref="UpdateAsync"/>. </summary>
public class DepartmentApi : NamedCrudApi<Department, Department, Department>
{
    protected override CrudStore<Department> Store(TimeEntryContext context) => CrudStore.For(new DepartmentRepo(context));
    protected override int KeyOf(Department row) => row.DepartmentId;
    protected override Department ToEntity(Department input) => input;
    protected override Department ToOutput(Department row) => row;

    protected override async Task<IResult> ListAsync(CrudCall call, ListQuery query) =>
        TypedResults.Ok(await new DepartmentRepo(call.Context).GetAllIncludeTeams());

    protected override Task<List<Department>> FindByNameAsync(TimeEntryContext context, string name) => new DepartmentRepo(context).GetByName(name);

    /// <summary>
    /// The department and its team list are saved together (B10): a team sent with an id is renamed or changed, one sent without an id is added, one left out of
    /// the list is removed unless something still uses it, and no list at all leaves the teams alone. Everything is one save, so it succeeds or fails as a whole.
    /// </summary>
    protected override async Task<IResult> UpdateAsync(CrudCall call, int id, Department updatedRow)
    {
        TimeEntryContext context = call.Context;
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

        return TypedResults.Ok(stored); // the saved department with its current teams
    }
}
