using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection.Metadata;

namespace TimeEntry.Common.Data.Entities;

public class E_TimeSheet
{
    #region Omitted
    [Key]
    [Display(Order = -1, AutoGenerateField = true)]
    public required int TimeSheetId { get; set; }

    [ForeignKey(nameof(Employee))]
    [Display(Order = -1, AutoGenerateField = false)]
    public required int EmployeeId { get; set; }
    #endregion Omitted

    // entities
    public Employee? Employee { get; set; }

    // list of details
    public List<E_TimeSheetDetail>? TimeSheetDetails { get; set; }

    [Display(Name = "When Entered", Description = "When Entered")]
    [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
    [DataType(DataType.Date)]
    public required DateTime WhenEntered { get; set; }

    [Display(Order = -1, Name = "Notes", Description = "Notes")]
    [StringLength(200)]
    [DataType(DataType.MultilineText)]
    public string? Notes { get; set; } = "";
}