using System.ComponentModel.DataAnnotations.Schema;
using TimeEntry.Common.Enums;

namespace TimeEntry.Common.Data.Entities;

public class TimeEntryUser
{
    #region Omitted
    [Key]
    [Display(Order = -1, AutoGenerateField = true)]
    public required int TimeEntryUserId { get; set; }

    [ForeignKey(nameof(SY_Role))]
    [Display(Order = -1, AutoGenerateField = false)]
    public required int SY_RoleId { get; set; }

    [ForeignKey(nameof(Employee))]
    [Display(Order = -1, AutoGenerateField = false)]
    public int? EmployeeId { get; set; }
    #endregion Omitted

    // entities
    public Employee? Employee { get; set; }

    // enums
    public SY_Role SY_Role { get; set; }


    [Display(Name = "User Name", Description = "User Name")]
    [StringLength(50)]
    public required string UserName { get; set; }

    [Display(Name = "Pword", Description = "Pword")]
    [StringLength(50)]
    public required string Pword { get; set; }

    [Display(Name = "Hint", Description = "Hint")]
    [StringLength(50)]
    public required string Hint { get; set; }

    [Display(Name = "Answer", Description = "Answer")]
    [StringLength(50)]
    public required string Answer { get; set; }

    [Display(Name = "Two Factor Auth", Description = "Two Factor Auth")]
    public required bool IsTwoFactorAuth { get; set; } = false;

    [Display(Name = "Phone Number", Description = "Phone Number")]
    [StringLength(50)]
    [DataType(DataType.PhoneNumber)]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Active", Description = "Active")]
    public required bool IsActive { get; set; } = true;

    [Display(Name = "When Left", Description = "When Left")]
    [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
    [DataType(DataType.Date)]
    public DateTime? WhenLeft { get; set; }
}