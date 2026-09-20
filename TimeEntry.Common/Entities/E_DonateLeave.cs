using System.ComponentModel.DataAnnotations.Schema;

namespace TimeEntry.Common.Entities;

public class E_DonateLeave : BaseEntity
{
    #region Omitted
    [Key]
    [Display(Order = -1, AutoGenerateField = true)]
    public required int DonateLeaveId { get; set; }

    [ForeignKey(nameof(DonateFrom_Employee))]
    [Display(Order = -1, AutoGenerateField = false)]
    public required int DonateFrom_EmployeeId { get; set; }

    [ForeignKey(nameof(DonateTo_Employee))]
    [Display(Order = -1, AutoGenerateField = false)]
    public required int DonateTo_EmployeeId { get; set; }
    #endregion Omitted

    // entities
    public Employee? DonateFrom_Employee { get; set; }
    public Employee? DonateTo_Employee { get; set; }

    [Display(Name = "When Donated", Description = "When Donated")]
    [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
    [DataType(DataType.Date)]
    public required DateTime WhenDonated { get; set; }

    [Display(Name = "Hours Donated", Description = "Hours Donated")]
    public required int HoursDonated { get; set; }

    [Display(Order = -1, Name = "Note", Description = "Note")]
    [StringLength(100)]
    [DataType(DataType.MultilineText)]
    public string? Note { get; set; }
}
