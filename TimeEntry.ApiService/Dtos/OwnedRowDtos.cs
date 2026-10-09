using System.ComponentModel.DataAnnotations;

namespace TimeEntry.ApiService.Dtos;

// What goes over the wire for an employee's own rows. Each row has two shapes:
//   XxxDto  - what the API returns: plain columns, no navigation properties.
//   XxxSave - what the API accepts: only the columns a caller may set. Values the server owns (when a request was made,
//             the status date) are filled in by the handler, never read from the body.
// Property names match the entities so the UI's JSON does not change.

// ---------- Time sheet ----------
public record TimeSheetDto(int TimeSheetId, int EmployeeId, DateTime WhenEntered, string? Notes)
{
    public static TimeSheetDto From(E_TimeSheet r) => new(r.TimeSheetId, r.EmployeeId, r.WhenEntered, r.Notes);
}

public record TimeSheetSave(int TimeSheetId, int EmployeeId, DateTime WhenEntered, [property: StringLength(200)] string? Notes)
{
    public E_TimeSheet ToEntity() => new() { TimeSheetId = TimeSheetId, EmployeeId = EmployeeId, WhenEntered = WhenEntered, Notes = Notes };
}

// ---------- Time sheet detail (the read shape is TimeSheetDetailRow, with the project and task names) ----------
public record TimeSheetDetailSave(
    int TimeSheetDetailId, int E_TimeSheetId, int ProjectId, int ProjectTaskId,
    [property: Range(0, 24)] decimal SundayHours, [property: Range(0, 24)] decimal MondayHours, [property: Range(0, 24)] decimal TuesdayHours,
    [property: Range(0, 24)] decimal WednesdayHours, [property: Range(0, 24)] decimal ThursdayHours, [property: Range(0, 24)] decimal FridayHours,
    [property: Range(0, 24)] decimal SaturdayHours, [property: StringLength(200)] string? Notes)
{
    public E_TimeSheetDetail ToEntity() => new()
    {
        TimeSheetDetailId = TimeSheetDetailId, E_TimeSheetId = E_TimeSheetId, ProjectId = ProjectId, ProjectTaskId = ProjectTaskId,
        SundayHours = SundayHours, MondayHours = MondayHours, TuesdayHours = TuesdayHours, WednesdayHours = WednesdayHours,
        ThursdayHours = ThursdayHours, FridayHours = FridayHours, SaturdayHours = SaturdayHours, Notes = Notes,
    };
}

// ---------- Request ----------
public record RequestDto(
    int RequestId, int EmployeeId, int SY_RequestStatusTypeId, int? ClearanceTypeId, int? OvertimeTypeId, int? LeaveTypeId, int? ExpenseTypeId,
    DateTime WhenRequested, string? Reason, DateTime? LeaveStart, DateTime? LeaveEnd, int? OvertimeHrsRequested, DateTime? StatusDate)
{
    public static RequestDto From(E_Request r) => new(
        r.RequestId, r.EmployeeId, r.SY_RequestStatusTypeId, r.ClearanceTypeId, r.OvertimeTypeId, r.LeaveTypeId, r.ExpenseTypeId,
        r.WhenRequested, r.Reason, r.LeaveStart, r.LeaveEnd, r.OvertimeHrsRequested, r.StatusDate);
}

/// <summary> WhenRequested and StatusDate are not here on purpose: the server sets both. </summary>
public record RequestSave(
    int RequestId, int EmployeeId, int SY_RequestStatusTypeId, int? ClearanceTypeId, int? OvertimeTypeId, int? LeaveTypeId, int? ExpenseTypeId,
    [property: StringLength(200)] string? Reason, DateTime? LeaveStart, DateTime? LeaveEnd, int? OvertimeHrsRequested)
{
    public E_Request ToEntity() => new()
    {
        RequestId = RequestId, EmployeeId = EmployeeId, SY_RequestStatusTypeId = SY_RequestStatusTypeId, ClearanceTypeId = ClearanceTypeId,
        OvertimeTypeId = OvertimeTypeId, LeaveTypeId = LeaveTypeId, ExpenseTypeId = ExpenseTypeId, Reason = Reason,
        LeaveStart = LeaveStart, LeaveEnd = LeaveEnd, OvertimeHrsRequested = OvertimeHrsRequested,
        WhenRequested = default, // set by the handler
    };
}

// ---------- Leave donation ----------
public record DonateLeaveDto(int DonateLeaveId, int DonateFrom_EmployeeId, int DonateTo_EmployeeId, DateTime WhenDonated, int HoursDonated, string? Note)
{
    public static DonateLeaveDto From(E_DonateLeave r) => new(r.DonateLeaveId, r.DonateFrom_EmployeeId, r.DonateTo_EmployeeId, r.WhenDonated, r.HoursDonated, r.Note);
}

public record DonateLeaveSave(
    int DonateLeaveId, int DonateFrom_EmployeeId, int DonateTo_EmployeeId, DateTime WhenDonated, [property: Range(1, int.MaxValue)] int HoursDonated,
    [property: StringLength(100)] string? Note)
{
    public E_DonateLeave ToEntity() => new()
    {
        DonateLeaveId = DonateLeaveId, DonateFrom_EmployeeId = DonateFrom_EmployeeId, DonateTo_EmployeeId = DonateTo_EmployeeId,
        WhenDonated = WhenDonated, HoursDonated = HoursDonated, Note = Note,
    };
}

// ---------- Expense sheet and its details ----------
public record ExpenseDetailDto(
    int RequestExpenseDetailId, int E_RequestExpenseSheetId, int ExpenseTypeId, DateTime ExpenseDate, decimal ReimbursableAmount, bool ReceiptProvided,
    string? AttachedReceiptFilePath, string? VendorName, string? Notes, int? LodgingNights, int? MilesForPerDiem, string? ExcuseForNoReceipt,
    DateTime? FlightDeparture, DateTime? FlightReturn)
{
    public static ExpenseDetailDto From(E_RequestExpenseDetail r) => new(
        r.RequestExpenseDetailId, r.E_RequestExpenseSheetId, r.ExpenseTypeId, r.ExpenseDate, r.ReimbursableAmount, r.ReceiptProvided,
        r.AttachedReceiptFilePath, r.VendorName, r.Notes, r.LodgingNights, r.MilesForPerDiem, r.ExcuseForNoReceipt, r.FlightDeparture, r.FlightReturn);
}

public record ExpenseDetailSave(
    int RequestExpenseDetailId, int E_RequestExpenseSheetId, int ExpenseTypeId, DateTime ExpenseDate, [property: Range(0, 1_000_000)] decimal ReimbursableAmount,
    bool ReceiptProvided, [property: StringLength(200)] string? AttachedReceiptFilePath, [property: StringLength(50)] string? VendorName,
    [property: StringLength(200)] string? Notes, int? LodgingNights, int? MilesForPerDiem, [property: StringLength(200)] string? ExcuseForNoReceipt,
    DateTime? FlightDeparture, DateTime? FlightReturn)
{
    public E_RequestExpenseDetail ToEntity() => new()
    {
        RequestExpenseDetailId = RequestExpenseDetailId, E_RequestExpenseSheetId = E_RequestExpenseSheetId, ExpenseTypeId = ExpenseTypeId,
        ExpenseDate = ExpenseDate, ReimbursableAmount = ReimbursableAmount, ReceiptProvided = ReceiptProvided,
        AttachedReceiptFilePath = AttachedReceiptFilePath, VendorName = VendorName, Notes = Notes, LodgingNights = LodgingNights,
        MilesForPerDiem = MilesForPerDiem, ExcuseForNoReceipt = ExcuseForNoReceipt, FlightDeparture = FlightDeparture, FlightReturn = FlightReturn,
    };
}

public record ExpenseSheetDto(int RequestExpenseSheetId, int ProjectId, int EmployeeId, string? Notes, List<ExpenseDetailDto>? ExpenseDetails)
{
    public static ExpenseSheetDto From(E_RequestExpenseSheet r) =>
        new(r.RequestExpenseSheetId, r.ProjectId, r.EmployeeId, r.Notes, r.ExpenseDetails?.Select(ExpenseDetailDto.From).ToList());
}

/// <summary> The details are saved through their own endpoint, not through the sheet. </summary>
public record ExpenseSheetSave(int RequestExpenseSheetId, int ProjectId, int EmployeeId, [property: StringLength(200)] string? Notes)
{
    public E_RequestExpenseSheet ToEntity() => new() { RequestExpenseSheetId = RequestExpenseSheetId, ProjectId = ProjectId, EmployeeId = EmployeeId, Notes = Notes };
}

// ---------- Response to a request ----------
public record ResponseDto(int ResponseId, int ManagerId, int E_RequestId, int ResponseTypeId, DateTime WhenResponded)
{
    public static ResponseDto From(Response r) => new(r.ResponseId, r.ManagerId, r.E_RequestId, r.ResponseTypeId, r.WhenResponded);
}

public record ResponseSave(int ResponseId, int ManagerId, int E_RequestId, int ResponseTypeId, DateTime WhenResponded)
{
    public Response ToEntity() => new() { ResponseId = ResponseId, ManagerId = ManagerId, E_RequestId = E_RequestId, ResponseTypeId = ResponseTypeId, WhenResponded = WhenResponded };
}
