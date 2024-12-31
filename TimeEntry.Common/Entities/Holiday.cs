namespace TimeEntry.Common.Entities;

public class Holiday : BaseEntity
{
    #region Omitted
    [Key]
    [Display(Order = -1, AutoGenerateField = true)]
    public required int HolidayId { get; set; }

    [Display(Order = -1, AutoGenerateField = false)]
    public required string SY_IsoCountry_Alpha3Code { get; set; }

    [Display(Order = -1, AutoGenerateField = false)]
    public int? SY_DisplayId { get; set; }
    #endregion Omitted

    [Display(Name = "Date", Description = "Date")]
    [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
    [DataType(DataType.Date)]
    public required DateTime Date { get; set; }

    [Display(Name = "Name", Description = "Name")]
    [StringLength(50)]
    [RegularExpression("([a-zA-Z]+)", ErrorMessage = "Enter only alphabetical letters for Name")]
    public required string Name { get; set; }

    public override string? ToString() => Name;
}