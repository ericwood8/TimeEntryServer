using System.ComponentModel.DataAnnotations.Schema;

namespace TimeEntry.Common.Data.Entities;

public class DepartmentTeam
{
    #region Omitted
    [Display(Order = -1, AutoGenerateField = false)]
    public required int DepartmentTeamId { get; set; }

    [ForeignKey(nameof(Department))]
    [Display(Order = -1, AutoGenerateField = false)]
    public required int DepartmentId { get; set; }
    #endregion Omitted

    [Display(Name = "Name", Description = "Name")]
    [StringLength(50)]
    [RegularExpression("([a-zA-Z]+)", ErrorMessage = "Enter only alphabetical letters for Name")]
    public required string Name { get; set; }

    [Display(Name = "Require X Employees", Description = "Require X Employees")]
    public int? RequireXEmployees { get; set; }

    [Display(Name = "Default", Description = "Default")]
    public required bool IsDefault { get; set; } = false;

    [Display(Name = "Active", Description = "Active")]
    public required bool IsActive { get; set; } = true;
}
