using System.Text.Json.Nodes;

namespace TimeEntry.Cli;

/// <summary> One kind of row the API serves: where it lives, which property is its id, and what a new row needs besides the caller's values. </summary>
public sealed record Resource(string Name, string Route, string IdProperty, bool NameSearch, string Description, bool HasIsActive = false, bool HasIsDefault = false)
{
    /// <summary> The properties a new row must carry for the API to accept the body: id 0 and the usual flags, unless the caller sets them. </summary>
    public JsonObject NewRowDefaults()
    {
        var defaults = new JsonObject { [IdProperty] = 0 };
        if (HasIsActive) defaults["isActive"] = true;
        if (HasIsDefault) defaults["isDefault"] = false;
        return defaults;
    }
}

public static class Resources
{
    // The routes are the API's own (it serves them without the /api the UI proxy adds).
    public static readonly IReadOnlyList<Resource> All =
    [
        new("departments", "/departments", "departmentId", true, "Departments, with their teams", HasIsActive: true, HasIsDefault: true),
        new("teams", "/departmentteams", "departmentTeamId", true, "Teams of a department", HasIsActive: true, HasIsDefault: true),
        new("employees", "/employees", "employeeId", true, "Employees", HasIsActive: true),
        new("holidays", "/holidays", "holidayId", true, "Holidays"),
        new("projects", "/projects", "projectId", true, "Projects", HasIsActive: true, HasIsDefault: true),
        new("tasks", "/projecttasks", "projectTaskId", true, "Tasks of a project", HasIsActive: true, HasIsDefault: true),
        new("users", "/timeentryusers", "timeEntryUserId", true, "Sign-in users (Admin only)", HasIsActive: true),
        new("restrictions", "/restrictleaves", "restrictLeaveId", false, "Dates when leave is restricted"),
        new("timesheets", "/timesheets", "timeSheetId", false, "Time sheets"),
        new("requests", "/requests", "requestId", false, "Leave, overtime and expense requests"),
        new("donations", "/donateleaves", "donateLeaveId", false, "Leave hours donated to a colleague"),
        new("expensesheets", "/expenseSheets", "requestExpenseSheetId", false, "Expense sheets"),
        new("expensedetails", "/expenseDetails", "requestExpenseDetailId", false, "Lines of an expense sheet"),
        new("responses", "/responses", "responseId", false, "Manager responses to requests"),
    ];

    public static Resource? Find(string name) => All.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
}
