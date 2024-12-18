using System.ComponentModel.DataAnnotations.Schema;

namespace TimeEntry.Common.Data.Entities;

public class E_RequestExpenseSheet
{
    #region Omitted
    [Key]
    [Display(Order = -1, AutoGenerateField = false)]
    public required int RequestExpenseSheetId { get; set; }

    [ForeignKey(nameof(Project))]
    [Display(Order = -1, AutoGenerateField = false)]
    public required int ProjectId { get; set; }

    [Display(Order = -1, AutoGenerateField = false)]
    public required int EmployeeId { get; set; }
    #endregion Omitted

    public required Employee Employee { get; set; }

    public required Project Project { get; set; }

    // list of details
    public List<E_RequestExpenseDetail>? ExpenseDetails { get; set; }

    [Display(Order = -1, Name = "Notes", Description = "Notes")]
    [StringLength(200)]
    [RegularExpression("([a-zA-Z]+)", ErrorMessage = "Enter only alphabetical letters for Name")]
    [DataType(DataType.MultilineText)]
    public string? Notes { get; set; }
}