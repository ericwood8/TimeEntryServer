using System.Security.Claims;
using TimeEntry.Common.Enums;

namespace TimeEntry.ApiService.Security;

/// <summary> Who is calling, read from the token. The caller's identity is never taken from the request body. </summary>
public static class CurrentUser
{
    public static int? UserId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue("sub"), out int id) ? id : null;

    public static int? EmployeeId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(TokenService.EmployeeIdClaim), out int id) ? id : null;

    public static SY_Role? Role(this ClaimsPrincipal user) =>
        Enum.TryParse(user.FindFirstValue("role"), out SY_Role role) ? role : null;

    public static bool IsAdmin(this ClaimsPrincipal user) => user.Role() == SY_Role.Admin;

    /// <summary> Admin and Human Resources may see and change every employee's rows and the reference data. </summary>
    public static bool CanManageAll(this ClaimsPrincipal user) => user.Role() is SY_Role.Admin or SY_Role.HumanResources;
}
