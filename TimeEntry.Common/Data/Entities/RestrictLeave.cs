using System.ComponentModel.DataAnnotations.Schema;

namespace TimeEntry.Common.Data.Entities;

public class RestrictLeave
{
    #region Omitted
    [Key]
    [Display(Order = -1, AutoGenerateField = true)]
    public required int RestrictLeaveId { get; set; }

    [ForeignKey(nameof(Department))]
    [Display(Order = -1, AutoGenerateField = false)]
    public int? DepartmentId { get; set; }

    [ForeignKey(nameof(DepartmentTeam))]
    [Display(Order = -1, AutoGenerateField = false)]
    public int? DepartmentTeamId { get; set; }

    [ForeignKey(nameof(Employee))]
    [Display(Order = -1, AutoGenerateField = false)]
    public int? EmployeeId { get; set; }
    #endregion Omitted

    // entities
    public Department? Department { get; set; }
    public DepartmentTeam? DepartmentTeam { get; set; }
    public Employee? Employee { get; set; }

    [Display(Name = "From Date Time", Description = "From Date Time")]
    [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
    [DataType(DataType.Date)]
    public required DateTime FromDateTime { get; set; }
     
    [Display(Name = "To Date Time", Description = "To Date Time")]
    [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
    [DataType(DataType.Date)]
    public required DateTime ToDateTime { get; set; }

    [Display(Name = "Reason", Description = "Reason")]
    [StringLength(50)]
    public string? Reason { get; set; }
}