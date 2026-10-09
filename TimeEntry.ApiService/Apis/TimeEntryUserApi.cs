using TimeEntry.ApiService.Security;
using TimeEntry.Common.Enums;

namespace TimeEntry.ApiService.Apis;

/// <summary>
/// Users, for Admin only (a filter on the group). A caller never sees or sends a stored password hash or answer hash: a new user needs a password and an answer,
/// which are hashed here, and a change keeps the stored ones unless new ones are sent.
/// </summary>
public class TimeEntryUserApi : NamedCrudApi<TimeEntryUser, TimeEntryUserSave, TimeEntryUserDto>
{
    protected override CrudStore<TimeEntryUser> Store(TimeEntryContext context) => CrudStore.For(new TimeEntryUserRepo(context));
    protected override int KeyOf(TimeEntryUser row) => row.TimeEntryUserId;
    protected override TimeEntryUserDto ToOutput(TimeEntryUser row) => TimeEntryUserDto.From(row);

    // the password and the answer are hashed in PrepareCreateAsync, so the entity starts without them
    protected override TimeEntryUser ToEntity(TimeEntryUserSave save) => new()
    {
        TimeEntryUserId = save.TimeEntryUserId,
        Name = save.Name ?? "",
        SY_RoleId = save.SY_RoleId,
        EmployeeId = save.EmployeeId,
        Pword = "",
        Hint = save.Hint?.Trim() ?? "",
        Answer = "",
        IsTwoFactorAuth = save.IsTwoFactorAuth,
        PhoneNumber = save.PhoneNumber,
        IsActive = save.IsActive,
        WhenLeft = save.WhenLeft,
    };

    protected override async Task<IResult> ListAsync(CrudCall call, ListQuery query) =>
        TypedResults.Ok((await new TimeEntryUserRepo(call.Context).GetAllActive()).Select(TimeEntryUserDto.From));

    protected override Task<List<TimeEntryUser>> FindByNameAsync(TimeEntryContext context, string name) => new TimeEntryUserRepo(context).GetByName(name);

    protected override async Task<IResult?> PrepareCreateAsync(CrudCall call, TimeEntryUser row, TimeEntryUserSave save)
    {
        if (await base.PrepareCreateAsync(call, row, save) is { } badName)
            return badName;
        if (!PasswordService.IsAcceptable(save.Password) || string.IsNullOrWhiteSpace(save.Answer))
            return ApiProblems.Invalid("A new user needs a password (at least 8 characters, with a letter and a digit) and a security answer.");
        if (!Enum.IsDefined(typeof(SY_Role), save.SY_RoleId))
            return ApiProblems.Invalid("That role does not exist.");

        var passwords = call.Services.GetRequiredService<PasswordService>();
        row.Pword = passwords.Hash(row, save.Password!);
        row.Answer = passwords.HashAnswer(row, save.Answer);
        return null;
    }

    /// <summary> Only the listed columns are copied, so the stored Pword and Answer survive unless a new one is sent. </summary>
    protected override async Task<IResult> UpdateAsync(CrudCall call, int id, TimeEntryUserSave save)
    {
        if (save.TimeEntryUserId != id)
            return ApiProblems.IdMismatch(); // 400 error if the id in the URL and the id in the body disagree

        string name = (save.Name ?? "").Trim();
        if (name.IsNameBad())
            return ApiProblems.BadName(); // 400 error if bad characters or empty
        if (!Enum.IsDefined(typeof(SY_Role), save.SY_RoleId))
            return ApiProblems.Invalid("That role does not exist.");
        if (!string.IsNullOrEmpty(save.Password) && !PasswordService.IsAcceptable(save.Password))
            return ApiProblems.Invalid("The password needs at least 8 characters, with a letter and a digit.");

        TimeEntryUserRepo repo = new(call.Context);
        TimeEntryUser? row = await repo.GetByIdAsync(id);
        if (row == null)
            return ApiProblems.NotFound(); // 404 error if there is no row with that id
        if (save.IsActive && await repo.IsDupOnUpdateAsync(id, name))
            return ApiProblems.DuplicateName(); // 422 error if another active user has that name

        var passwords = call.Services.GetRequiredService<PasswordService>();
        row.Name = name;
        row.SY_RoleId = save.SY_RoleId;
        row.EmployeeId = save.EmployeeId;
        row.Hint = save.Hint?.Trim() ?? row.Hint;
        row.IsTwoFactorAuth = save.IsTwoFactorAuth;
        row.PhoneNumber = save.PhoneNumber;
        row.IsActive = save.IsActive;
        row.WhenLeft = save.WhenLeft;
        if (!string.IsNullOrEmpty(save.Password))
            row.Pword = passwords.Hash(row, save.Password);
        if (!string.IsNullOrWhiteSpace(save.Answer))
            row.Answer = passwords.HashAnswer(row, save.Answer);

        await call.Context.SaveChangesAsync();
        return TypedResults.Ok(TimeEntryUserDto.From(row));
    }
}
