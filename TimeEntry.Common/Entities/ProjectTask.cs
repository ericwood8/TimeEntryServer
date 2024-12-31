using System.ComponentModel.DataAnnotations.Schema;

namespace TimeEntry.Common.Entities;

public class ProjectTask : BaseNameActiveEntity
{
    #region Omitted
    [Key]
    [Display(Order = -1, AutoGenerateField = true)]
    public required int ProjectTaskId { get; set; }

    [ForeignKey(nameof(Project))]
    [Display(Order = -1, AutoGenerateField = false)]
    public required int ProjectId { get; set; }

    [Display(Order = -1, AutoGenerateField = false)]
    public DateTime? Inactivated { get; set; }
    #endregion Omitted 

    [Display(Name = "Default", Description = "Default")]
    public required bool IsDefault { get; set; } = false;
}