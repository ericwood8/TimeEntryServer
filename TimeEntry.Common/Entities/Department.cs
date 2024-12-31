namespace TimeEntry.Common.Entities;

public class Department : BaseNameActiveEntity
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

    [Display(Name = "Default", Description = "Default")]
    public required bool IsDefault { get; set; } = false;
}