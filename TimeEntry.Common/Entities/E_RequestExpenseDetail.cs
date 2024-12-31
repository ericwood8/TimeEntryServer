namespace TimeEntry.Common.Entities;

public class E_RequestExpenseDetail : BaseEntity
{
    #region Omitted
    [Key]
    [Display(Order = -1, AutoGenerateField = true)]
    public required int RequestExpenseDetailId { get; set; }

    [Display(Order = -1, AutoGenerateField = false)]
    public required int E_RequestExpenseSheetId { get; set; }

    [Display(Order = -1, AutoGenerateField = false)]
    public required int ExpenseTypeId { get; set; }
    #endregion Omitted

    public required E_RequestExpenseSheet E_RequestExpenseSheet { get; set; }

    [Display(Name = "Expense Date", Description = "Expense Date")]
    [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
    [DataType(DataType.Date)]
    public required DateTime ExpenseDate { get; set; }

    [Display(Name = "Reimbursable Amount", Description = "Reimbursable Amount")]
    [DisplayFormat(ApplyFormatInEditMode = true)]
    [DataType(DataType.Currency)]
    public required decimal ReimbursableAmount { get; set; }

    [Display(Name = "Receipt Provided", Description = "Receipt Provided")]
    public required bool ReceiptProvided { get; set; }

    [Display(Name = "Attached Receipt File Path", Description = "Attached Receipt File Path")]
    [StringLength(200)]
    [DataType(DataType.MultilineText)]
    public string? AttachedReceiptFilePath { get; set; }

    [Display(Name = "Vendor Name", Description = "Vendor Name")]
    [StringLength(50)]
    public string? VendorName { get; set; }

    [Display(Order = -1, Name = "Notes", Description = "Notes")]
    [StringLength(200)]
    [DataType(DataType.MultilineText)]
    public string? Notes { get; set; } = "";

    [Display(Name = "Lodging Nights", Description = "Lodging Nights")]
    public int? LodgingNights { get; set; }

    [Display(Name = "Miles For Per Diem", Description = "Miles For Per Diem")]
    public int? MilesForPerDiem { get; set; }

    [Display(Name = "Excuse For No Receipt", Description = "Excuse For No Receipt")]
    [StringLength(200)]
    [DataType(DataType.MultilineText)]
    public string? ExcuseForNoReceipt { get; set; }

    [Display(Name = "Flight Departure", Description = "Flight Departure")]
    [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
    [DataType(DataType.Date)]
    public DateTime? FlightDeparture { get; set; }

    [Display(Name = "Flight Return", Description = "Flight Return")]
    [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
    [DataType(DataType.Date)]
    public DateTime? FlightReturn { get; set; }
}