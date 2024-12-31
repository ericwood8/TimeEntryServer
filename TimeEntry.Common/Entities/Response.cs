using System.ComponentModel.DataAnnotations.Schema;
using TimeEntry.Common.Enums;

namespace TimeEntry.Common.Entities;

public class Response : BaseEntity
{
    #region Omitted
    [Key]
    [Display(Order = -1, AutoGenerateField = true)]
    public required int ResponseId { get; set; }

    [ForeignKey(nameof(Employee))]
    [Display(Order = -1, AutoGenerateField = false)]
    public required int ManagerId { get; set; }

    [ForeignKey(nameof(E_Request))]
    [Display(Order = -1, AutoGenerateField = false)]
    public required int E_RequestId { get; set; }

    [ForeignKey(nameof(SY_ResponseType))]
    [Display(Order = -1, AutoGenerateField = false)]
    public required int ResponseTypeId { get; set; }
    #endregion Omitted

    // entities
    public required Employee Manager { get; set; }
    public required E_Request E_Request { get; set; }

    // enums
    public required SY_ResponseType ResponseType { get; set; }

    [Display(Name = "When Responded", Description = "When Responded")]
    [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
    [DataType(DataType.Date)]
    public required DateTime WhenResponded { get; set; }
}