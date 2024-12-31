using System.ComponentModel.DataAnnotations.Schema;

namespace TimeEntry.Common.Entities;

public class E_TimeSheetDetail : BaseEntity
{
    #region Omitted
    [Key]
    [Display(Order = -1, AutoGenerateField = true)]
    public required int TimeSheetDetailId { get; set; }

    [ForeignKey(nameof(E_TimeSheet))]
    [Display(Order = -1, AutoGenerateField = false)]
    public required int E_TimeSheetId { get; set; }

    [ForeignKey(nameof(Project))]
    [Display(Order = -1, AutoGenerateField = false)]
    public required int ProjectId { get; set; }

    [ForeignKey(nameof(ProjectTask))]
    [Display(Order = -1, AutoGenerateField = false)]
    public required int ProjectTaskId { get; set; }
    #endregion Omitted

    // entities 
    public Project? Project { get; set; }
    public ProjectTask? ProjectTask { get; set; }

    [Display(Name = "Sunday Hours", Description = "Sunday Hours")]
    [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "#0.00")]
    [Range(0, 24)]
    public required decimal SundayHours { get; set; }

    [Required]
    [Display(Name = "Monday Hours", Description = "Monday Hours")]
    [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "#0.00")]
    [Range(0, 24)]
    public required decimal MondayHours { get; set; }

    [Required]
    [Display(Name = "Tuesday Hours", Description = "Tuesday Hours")]
    [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "#0.00")]
    [Range(0, 24)]
    public required decimal TuesdayHours { get; set; }

    [Display(Name = "Wednesday Hours", Description = "Wednesday Hours")]
    [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "#0.00")]
    [Range(0, 24)]
    public required decimal WednesdayHours { get; set; }

    [Display(Name = "Thursday Hours", Description = "Thursday Hours")]
    [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "#0.00")]
    [Range(0, 24)]
    public required decimal ThursdayHours { get; set; }

    [Display(Name = "Friday Hours", Description = "Friday Hours")]
    [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "#0.00")]
    [Range(0, 24)]
    public required decimal FridayHours { get; set; }

    [Display(Name = "Saturday Hours", Description = "Saturday Hours")]
    [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "#0.00")]
    [Range(0, 24)]
    public required decimal SaturdayHours { get; set; }

    [Display(Order = -1, Name = "Notes", Description = "Notes")]
    [StringLength(200)]
    [DataType(DataType.MultilineText)]
    public string Notes { get; set; } = "";
}