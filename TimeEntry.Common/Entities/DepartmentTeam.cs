using System.ComponentModel.DataAnnotations.Schema;

namespace TimeEntry.Common.Entities;

public class DepartmentTeam : BaseNameActiveEntity
{
    #region Omitted
    [Display(Order = -1, AutoGenerateField = false)]
    public required int DepartmentTeamId { get; set; }

    [ForeignKey(nameof(Department))]
    [Display(Order = -1, AutoGenerateField = false)]
    public required int DepartmentId { get; set; }
    #endregion Omitted

    [Display(Name = "Require X Employees", Description = "Require X Employees")]
    public int? RequireXEmployees { get; set; }

    [Display(Name = "Default", Description = "Default")]
    public required bool IsDefault { get; set; } = false;
}
