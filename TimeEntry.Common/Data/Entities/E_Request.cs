using System.ComponentModel.DataAnnotations.Schema;
using TimeEntry.Common.Enums;

namespace TimeEntry.Common.Data.Entities;

public class E_Request
{
    #region Omitted
    [Key]
    [Display(Order = -1, AutoGenerateField = true)]
    public required int RequestId { get; set; }

    [ForeignKey(nameof(Employee))]
    [Display(Order = -1, AutoGenerateField = false)]
    public required int EmployeeId { get; set; }

    [Display(Order = -1, AutoGenerateField = false)]
    public required int SY_RequestStatusTypeId { get; set; }

    [Display(Order = -1, AutoGenerateField = false)]
    public int? ClearanceTypeId { get; set; }

    [Display(Order = -1, AutoGenerateField = false)]
    public int? OvertimeTypeId { get; set; }

    [Display(Order = -1, AutoGenerateField = false)]
    public int? LeaveTypeId { get; set; }

    [Display(Order = -1, AutoGenerateField = false)]
    public int? ExpenseTypeId { get; set; }
    #endregion Omitted

    // Entities
    public Employee Employee { get; set; }

    // Enums
    //public SY_RequestStatusType SY_RequestStatusType { get; set; }
    //public ClearanceType ClearanceType { get; set; }
    //public OvertimeType OvertimeType { get; set; }
    //public LeaveType LeaveType { get; set; }
    //public ExpenseType ExpenseType { get; set; }

    [Display(Name = "When Requested", Description = "When Requested")]
    [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
    [DataType(DataType.Date)]
    public required DateTime WhenRequested { get; set; }

    [Display(Name = "Reason", Description = "Reason")]
    [StringLength(200)]
    [DataType(DataType.MultilineText)]
    public string? Reason { get; set; } = "";

    [Display(Name = "Leave Start", Description = "Leave Start")]
    [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
    [DataType(DataType.Date)]
    public DateTime? LeaveStart { get; set; }

    [Display(Name = "Leave End", Description = "Leave End")]
    [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
    [DataType(DataType.Date)]
    public DateTime? LeaveEnd { get; set; }

    [Display(Name = "Overtime Hrs Requested", Description = "Overtime Hrs Requested")]
    public int? OvertimeHrsRequested { get; set; }

    [Display(Name = "StatusDate", Description = "LeaveStatusDate")]
    [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
    [DataType(DataType.Date)]
    public DateTime? StatusDate { get; set; }
}