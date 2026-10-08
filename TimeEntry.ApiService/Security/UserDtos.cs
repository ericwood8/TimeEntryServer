using TimeEntry.Common.Enums;

namespace TimeEntry.ApiService.Security;

/// <summary> What the API shows about a user. Pword, Hint and Answer are never part of it. </summary>
public record TimeEntryUserDto(
    int TimeEntryUserId, string Name, int SY_RoleId, SY_Role SY_Role, int? EmployeeId,
    bool IsTwoFactorAuth, string? PhoneNumber, bool IsActive, DateTime? WhenLeft)
{
    public static TimeEntryUserDto From(TimeEntryUser u) =>
        new(u.TimeEntryUserId, u.Name, u.SY_RoleId, u.SY_Role, u.EmployeeId, u.IsTwoFactorAuth, u.PhoneNumber, u.IsActive, u.WhenLeft);
}

/// <summary> What a caller sends to add or change a user. On a change, a blank Password or Answer keeps the stored one. </summary>
public record TimeEntryUserSave(
    int TimeEntryUserId, string Name, int SY_RoleId, int? EmployeeId, string? Password, string? Hint, string? Answer,
    bool IsTwoFactorAuth, string? PhoneNumber, bool IsActive, DateTime? WhenLeft);

public record LoginRequest(string UserName, string Password);

public record LoginResponse(string Token, DateTime ExpiresUtc, TimeEntryUserDto User);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
