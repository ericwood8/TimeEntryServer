using System.ComponentModel.DataAnnotations.Schema;

namespace TimeEntry.Common.Entities;

public class E_RequestExpenseSheet : BaseEntity
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

    public Employee? Employee { get; set; }

    public Project? Project { get; set; }

    // list of details
    public List<E_RequestExpenseDetail>? ExpenseDetails { get; set; }

    [Display(Order = -1, Name = "Notes", Description = "Notes")]
    [StringLength(200)]
    [DataType(DataType.MultilineText)]
    public string? Notes { get; set; }
}