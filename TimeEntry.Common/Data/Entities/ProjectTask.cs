using System.ComponentModel.DataAnnotations.Schema;

namespace TimeEntry.Common.Data.Entities;

public class ProjectTask
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

    [Display(Name = "Name", Description = "Name")]
    [StringLength(50)]
    [RegularExpression("([a-zA-Z]+)", ErrorMessage = "Enter only alphabetical letters for Name")]
    public required string Name { get; set; }

    [Display(Name = "Default", Description = "Default")]
    public required bool IsDefault { get; set; } = false;

    [Display(Name = "Active", Description = "Active")]
    public required bool IsActive { get; set; } = true;
}