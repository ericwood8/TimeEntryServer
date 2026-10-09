namespace TimeEntry.ApiService.Dtos;

/// <summary>
/// What a drop-down or a name column needs of an employee: the id and the name (spec item E12). GET /employees carries each person's manager, department and
/// team as well, which is 792 KB for 510 people.
/// </summary>
public record EmployeeLookup(int EmployeeId, string Name);
