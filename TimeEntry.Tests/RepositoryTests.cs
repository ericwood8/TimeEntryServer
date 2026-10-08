using System.ComponentModel.DataAnnotations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TimeEntry.ApiService.Apis;
using TimeEntry.Common.Context;
using TimeEntry.Common.Entities;
using TimeEntry.Common.Repositories;

namespace TimeEntry.Tests;

/// <summary> Repository tests on in-memory SQLite (unlike the EF in-memory provider it enforces keys and constraints). No SQL Server needed. </summary>
[TestClass]
public class RepositoryTests
{
    SqliteConnection _connection = null!;
    TimeEntryContext _context = null!;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<TimeEntryContext>().UseSqlite(_connection).Options;
        _context = new TimeEntryContext(options);
        _context.Database.EnsureCreated();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    static Project NewProject(string name, bool active = true) => new() { ProjectId = 0, Name = name, IsActive = active, IsDefault = false };

    // ---- B01 / B08: ranges are saved before the call returns ----

    [TestMethod]
    public async Task AddRangeAsync_SavesBeforeReturning()
    {
        ProjectRepo repo = new(_context);
        await repo.AddRangeAsync([NewProject("Alpha"), NewProject("Beta")]);

        using var other = new TimeEntryContext(new DbContextOptionsBuilder<TimeEntryContext>().UseSqlite(_connection).Options);
        Assert.AreEqual(2, await other.Project.CountAsync());
    }

    [TestMethod]
    public async Task RemoveRangeAsync_SavesBeforeReturning()
    {
        ProjectRepo repo = new(_context);
        await repo.AddRangeAsync([NewProject("Alpha"), NewProject("Beta")]);
        await repo.RemoveRangeAsync(await repo.GetListAsync(p => p.Name == "Alpha"));

        using var other = new TimeEntryContext(new DbContextOptionsBuilder<TimeEntryContext>().UseSqlite(_connection).Options);
        Assert.AreEqual(1, await other.Project.CountAsync());
    }

    [TestMethod]
    public void Repositories_DoNotOwnTheContext()
    {
        Assert.IsFalse(typeof(IDisposable).IsAssignableFrom(typeof(GenericRepo<E_TimeSheet>)));
        Assert.IsFalse(typeof(IDisposable).IsAssignableFrom(typeof(NameActiveRepo<Project>)));
    }

    // ---- B06: one name rule for create and update ----

    [TestMethod]
    public async Task AddAsync_RejectsDuplicateNameEvenWithSpaces()
    {
        ProjectRepo repo = new(_context);
        Assert.IsTrue(await repo.AddAsync(NewProject("Alpha")));
        Assert.IsFalse(await repo.AddAsync(NewProject("  Alpha ")));
    }

    [TestMethod]
    public async Task AddAsync_InactiveRowDoesNotBlockTheName()
    {
        ProjectRepo repo = new(_context);
        Assert.IsTrue(await repo.AddAsync(NewProject("Alpha", active: false)));
        Assert.IsTrue(await repo.AddAsync(NewProject("Alpha")));
    }

    [TestMethod]
    public async Task UpdateAsync_RenameToAnExistingNameIsRefused()
    {
        ProjectRepo repo = new(_context);
        await repo.AddAsync(NewProject("Alpha"));
        var beta = NewProject("Beta");
        await repo.AddAsync(beta);

        beta.Name = "Alpha";
        Assert.IsNull(await repo.UpdateAsync(beta.ProjectId, beta));
    }

    [TestMethod]
    public async Task UpdateAsync_KeepingTheOwnNameIsFine()
    {
        ProjectRepo repo = new(_context);
        var alpha = NewProject("Alpha");
        await repo.AddAsync(alpha);

        alpha.IsDefault = true;
        var saved = await repo.UpdateAsync(alpha.ProjectId, alpha);
        Assert.IsNotNull(saved);
        Assert.IsTrue(saved.IsDefault);
    }

    [TestMethod]
    [DataRow("Alpha", false)]
    [DataRow("O'Brien", false)]
    [DataRow("Jean-Luc 2", false)]
    [DataRow("Alpha!", true)]
    [DataRow("   ", true)]
    public void IsNameBad_MatchesTheEntityRule(string name, bool bad)
    {
        Assert.AreEqual(bad, name.IsNameBad());

        var results = new List<ValidationResult>();
        bool entityOk = Validator.TryValidateProperty(name, new ValidationContext(NewProject("x")) { MemberName = nameof(Project.Name) }, results);
        Assert.AreEqual(!bad, entityOk, $"entity rule disagrees for '{name}'");
    }
}
