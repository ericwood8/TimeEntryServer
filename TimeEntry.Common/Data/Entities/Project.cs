namespace TimeEntry.Common.Data.Entities;

public class Project
{
    #region Omitted
    [Key]
    [Display(Order = -1, AutoGenerateField = true)]
    public required int ProjectId { get; set; }
    #endregion Omitted

    // list of details
    public List<ProjectTask>? Tasks { get; set; }

    [Display(Name = "Name", Description = "Name")]
    [StringLength(50)]
    [RegularExpression("([a-zA-Z]+)", ErrorMessage = "Enter only alphabetical letters for Name")]
    public required string Name { get; set; }

    [Display(Name = "Default", Description = "Default")]
    public required bool IsDefault { get; set; } = false;

    [Display(Name = "Active", Description = "Active")]
    public required bool IsActive { get; set; } = true;
}