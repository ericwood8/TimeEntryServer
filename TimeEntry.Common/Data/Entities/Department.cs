namespace TimeEntry.Common.Data.Entities;

public class Department
{
    #region Omitted
    [Key]
    [Display(Order = -1, AutoGenerateField = true)]
    public required int DepartmentId { get; set; }

    [Display(Order = -1, AutoGenerateField = false)]
    public int? SY_DisplayId { get; set; }
    #endregion Omitted

    // list of details
    public List<DepartmentTeam>? Teams { get; set; }

    [Display(Name = "Name", Description = "Name")]
    [StringLength(50)]
    [RegularExpression("([a-zA-Z]+)", ErrorMessage = "Enter only alphabetical letters for Name")]
    public required string Name { get; set; }

    [Display(Name = "Default", Description = "Default")]
    public required bool IsDefault { get; set; } = false;

    [Display(Name = "Active", Description = "Active")]
    public required bool IsActive { get; set; } = true;
}