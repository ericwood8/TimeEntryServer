namespace TimeEntry.Common.Data.Entities;

public class Employee
{
    #region Omitted
    [Key]
    [Display(Order = -1, AutoGenerateField = true)]
    public required int EmployeeId { get; set; }

    [Display(Order = -1, AutoGenerateField = false)]
    public int? ManagerId { get; set; }

    [Display(Order = -1, AutoGenerateField = false)]
    public required int DepartmentId { get; set; }

    [Display(Order = -1, AutoGenerateField = false)]
    public required int DepartmentTeamId { get; set; }
    #endregion Omitted

    // entities
    public Employee? Manager { get; set; }
    public Department? Department { get; set; }
    public DepartmentTeam? DepartmentTeam { get; set; }


    [Display(Name = "Name", Description = "Name")]
    [StringLength(200)]
    [DataType(DataType.MultilineText)]
    [RegularExpression("([a-zA-Z]+)", ErrorMessage = "Enter only alphabetical letters for Name")]
    public required string Name { get; set; }

    [Display(Name = "Leave Hours", Description = "Leave Hours")]
    public required int AvailableLeaveHours { get; set; }

    [Display(Name = "Donated Hrs Received", Description = "Donated Hrs Received")]
    public required int DonatedHrsReceived { get; set; }

    [Display(Name = "Active", Description = "Active")]
    public required bool IsActive { get; set; } = true;

    [Display(Name = "When Left", Description = "When Left")]
    [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
    [DataType(DataType.Date)]
    public DateTime? WhenLeft { get; set; }
}
