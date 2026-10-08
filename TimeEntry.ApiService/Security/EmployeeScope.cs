using System.Security.Claims;

namespace TimeEntry.ApiService.Security;

/// <summary> The employees whose time sheets, requests and the like the caller may see: themselves and the people who report to them. </summary>
public sealed class EmployeeScope
{
    private readonly int[] _employeeIds;

    public bool All { get; }

    private EmployeeScope(bool all, int[] employeeIds)
    {
        All = all;
        _employeeIds = employeeIds;
    }

    public bool Allows(int employeeId) => All || _employeeIds.Contains(employeeId);

    /// <summary> For use in a Where() on an employee id column; EF turns it into IN (...). </summary>
    public int[] EmployeeIds => _employeeIds;

    /// <summary> Null when the caller may use a row of that employee; otherwise the answer to send: 404 when there is no such row, 403 when it is someone else's. </summary>
    public IResult? Deny(int? ownerEmployeeId) =>
        ownerEmployeeId == null ? Results.NotFound() : Allows(ownerEmployeeId.Value) ? null : Results.Forbid();

    public static async Task<EmployeeScope> ForAsync(TimeEntryContext context, ClaimsPrincipal user)
    {
        if (user.CanManageAll())
            return new EmployeeScope(true, []);

        int? own = user.EmployeeId();
        if (own == null)
            return new EmployeeScope(false, []);

        List<int> ids = await context.Employee.AsNoTracking()
            .Where(e => e.ManagerId == own)
            .Select(e => e.EmployeeId)
            .ToListAsync();
        ids.Add(own.Value);
        return new EmployeeScope(false, [.. ids]);
    }
}
