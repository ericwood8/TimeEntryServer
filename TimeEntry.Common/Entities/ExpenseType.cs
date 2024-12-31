//namespace TimeEntry.Common.Data.Entities;

//public class ExpenseType
//{
//    #region Omitted
//    [Display(Order = -1, AutoGenerateField = false)]
//    public required int ExpenseTypeId { get; set; }

//    [Display(Order = -1, AutoGenerateField = false)]
//    public int? SY_DisplayId { get; set; }
//    #endregion Omitted

//    [Display(Name = "Name", Description = "Name")]
//    [StringLength(50)]
//    public required string Name { get; set; }

//    [Display(Name = "Per Diem", Description = "Per Diem")]
//    public required bool IsPerDiem { get; set; } = false;

//    [Display(Name = "Lodging", Description = "Lodging")]
//    public required bool IsLodging { get; set; } = false;

//    [Display(Name = "Car Rental", Description = "Car Rental")]
//    public required bool IsCarRental { get; set; } = false;

//    [Display(Name = "Airfare", Description = "Airfare")]
//    public required bool IsAirfare { get; set; } = false;

//    [Display(Name = "Meal", Description = "Meal")]
//    public required bool IsMeal { get; set; } = false;

//    [Display(Name = "Parking Or Tolls", Description = "Parking Or Tolls")]
//    public required bool IsParkingOrTolls { get; set; } = false;
//}