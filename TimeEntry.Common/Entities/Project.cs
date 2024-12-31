namespace TimeEntry.Common.Entities;

public class Project : BaseNameActiveEntity
{
    #region Omitted
    [Key]
    [Display(Order = -1, AutoGenerateField = true)]
    public required int ProjectId { get; set; }
    #endregion Omitted

    // list of details
    public List<ProjectTask>? Tasks { get; set; }

    [Display(Name = "Default", Description = "Default")]
    public required bool IsDefault { get; set; } = false;
}