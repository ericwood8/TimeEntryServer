using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TimeEntry.ApiService.Apis;
using TimeEntry.Common.Context;
using TimeEntry.Common.Entities;

namespace TimeEntry.Tests;

/// <summary> The paging, sorting and searching every big list uses (spec item E03), on in-memory SQLite: no SQL Server and no HTTP needed. </summary>
[TestClass]
public class ListQueryTests
{
    SqliteConnection _connection = null!;
    TimeEntryContext _context = null!;

    record Row(int Id, string Name);

    static readonly SortMap<Project> Sorts = new SortMap<Project>().Add("name", p => p.Name).Add("id", p => p.ProjectId);

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _context = new TimeEntryContext(new DbContextOptionsBuilder<TimeEntryContext>().UseSqlite(_connection).Options);
        _context.Database.EnsureCreated();
        // 25 projects: five names, each used five times, so a sort by name has ties that only the key can break
        _context.Project.AddRange(Enumerable.Range(1, 25).Select(i => new Project { ProjectId = 0, Name = $"Name {(char)('A' + i % 5)}", IsActive = true, IsDefault = false }));
        _context.SaveChanges();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    Task<IResult> List(ListQuery query, string defaultSort = "id:asc") =>
        _context.Project.ToResultAsync(query, Sorts, defaultSort, p => p.ProjectId,
            text => p => p.Name.Contains(text), p => new Row(p.ProjectId, p.Name));

    // the answer of TypedResults.Ok(...) / Results.Problem(...) without an HTTP pipeline
    static T Value<T>(IResult result) => (T)result.GetType().GetProperty("Value")!.GetValue(result)!;
    static int? Status(IResult result) => (int?)result.GetType().GetProperty("StatusCode")?.GetValue(result);
    static string Detail(IResult result)
    {
        object problem = result.GetType().GetProperty("ProblemDetails")!.GetValue(result)!;
        return (string)problem.GetType().GetProperty("Detail")!.GetValue(problem)!;
    }

    [TestMethod]
    public async Task Without_a_page_parameter_the_answer_is_the_whole_list()
    {
        var rows = Value<IEnumerable<Row>>(await List(new ListQuery())).ToList();

        Assert.HasCount(25, rows);
        CollectionAssert.AreEqual(rows.Select(r => r.Id).Order().ToList(), rows.Select(r => r.Id).ToList(), "the default sort is by id");
    }

    [TestMethod]
    public async Task Pages_hold_the_page_size_then_the_rest_and_never_repeat_a_row()
    {
        var first = Value<PaginatedItems<Row>>(await List(new ListQuery(PageIndex: 0, PageSize: 10, Sort: "name:asc")));
        var second = Value<PaginatedItems<Row>>(await List(new ListQuery(PageIndex: 1, PageSize: 10, Sort: "name:asc")));
        var third = Value<PaginatedItems<Row>>(await List(new ListQuery(PageIndex: 2, PageSize: 10, Sort: "name:asc")));

        Assert.AreEqual(25, first.Count);
        Assert.AreEqual(10, first.PageSize);
        Assert.AreEqual(10, first.Data.Count());
        Assert.AreEqual(10, second.Data.Count());
        Assert.AreEqual(5, third.Data.Count());
        var ids = first.Data.Concat(second.Data).Concat(third.Data).Select(r => r.Id).ToList();
        Assert.HasCount(25, ids.Distinct().ToList(), "ties in the name are broken by the key, so no row is on two pages and none is missed");
    }

    [TestMethod]
    public async Task A_page_past_the_end_is_empty_but_still_says_how_many_rows_there_are()
    {
        var page = Value<PaginatedItems<Row>>(await List(new ListQuery(PageIndex: 9, PageSize: 10)));

        Assert.AreEqual(25, page.Count);
        Assert.IsFalse(page.Data.Any());
    }

    [TestMethod]
    public async Task Sorting_descending_reverses_the_order_and_search_narrows_the_rows_and_the_count()
    {
        var desc = Value<PaginatedItems<Row>>(await List(new ListQuery(PageIndex: 0, PageSize: 25, Sort: "name:desc"))).Data.ToList();
        Assert.AreEqual("Name E", desc.First().Name);
        Assert.AreEqual("Name A", desc.Last().Name);

        var found = Value<PaginatedItems<Row>>(await List(new ListQuery(PageIndex: 0, PageSize: 10, Sort: "id", Search: " Name C ")));
        Assert.AreEqual(5, found.Count);
        Assert.IsTrue(found.Data.All(r => r.Name == "Name C"));

        var none = Value<PaginatedItems<Row>>(await List(new ListQuery(PageIndex: 0, PageSize: 10, Search: "zzz")));
        Assert.AreEqual(0, none.Count);
    }

    [TestMethod]
    public async Task The_page_size_is_capped_and_the_answer_says_which_size_was_used()
    {
        var page = Value<PaginatedItems<Row>>(await List(new ListQuery(PageIndex: 0, PageSize: 5000)));

        Assert.AreEqual(ListQuery.MaxPageSize, page.PageSize);
        Assert.AreEqual(25, page.Data.Count());
    }

    [TestMethod]
    public async Task A_page_size_alone_means_the_first_page_and_a_page_index_alone_the_default_size()
    {
        var sizeOnly = Value<PaginatedItems<Row>>(await List(new ListQuery(PageSize: 7)));
        Assert.AreEqual(0, sizeOnly.PageIndex);
        Assert.AreEqual(7, sizeOnly.Data.Count());

        var indexOnly = Value<PaginatedItems<Row>>(await List(new ListQuery(PageIndex: 1)));
        Assert.AreEqual(ListQuery.DefaultPageSize, indexOnly.PageSize);
    }

    [TestMethod]
    [DataRow(-1, 10, null)]
    [DataRow(0, 0, null)]
    [DataRow(0, -5, null)]
    [DataRow(0, 10, "password")]
    [DataRow(0, 10, "name:sideways")]
    public async Task A_bad_page_size_page_index_sort_or_direction_is_a_400(int pageIndex, int pageSize, string? sort)
    {
        var result = await List(new ListQuery(pageIndex, pageSize, sort));

        Assert.AreEqual(400, Status(result));
    }

    [TestMethod]
    public async Task An_unknown_sort_column_is_refused_with_the_columns_that_can_be_used()
    {
        var result = await List(new ListQuery(0, 10, "password"));

        StringAssert.Contains(Detail(result), "id, name");
    }

    [TestMethod]
    public async Task The_employee_ids_of_a_search_are_the_employees_whose_name_contains_the_text()
    {
        var dept = new Department { DepartmentId = 0, Name = "Dept", IsActive = true, IsDefault = false };
        _context.Department.Add(dept);
        await _context.SaveChangesAsync();
        var team = new DepartmentTeam { DepartmentTeamId = 0, DepartmentId = dept.DepartmentId, Name = "Team", IsActive = true, IsDefault = false };
        _context.DepartmentTeam.Add(team);
        await _context.SaveChangesAsync();
        _context.Employee.AddRange(
            new Employee { EmployeeId = 0, Name = "Pat Jones", IsActive = true, DepartmentId = dept.DepartmentId, DepartmentTeamId = team.DepartmentTeamId, AvailableLeaveHours = 0, DonatedHrsReceived = 0 },
            new Employee { EmployeeId = 0, Name = "Sam Smith", IsActive = true, DepartmentId = dept.DepartmentId, DepartmentTeamId = team.DepartmentTeamId, AvailableLeaveHours = 0, DonatedHrsReceived = 0 });
        await _context.SaveChangesAsync();

        // SQLite compares text case-sensitively; SQL Server (the default collation) does not, so the case here matches
        int[] ids = await _context.EmployeeIdsNamedAsync(new ListQuery(Search: " Jones "));
        Assert.HasCount(1, ids);
        Assert.IsEmpty(await _context.EmployeeIdsNamedAsync(new ListQuery()), "no search text: no ids to look up");
    }
}
