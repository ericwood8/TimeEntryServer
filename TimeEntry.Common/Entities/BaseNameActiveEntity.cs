namespace TimeEntry.Common.Entities;

/// <summary>  BaseEntity + Name and IsActive columns </summary>
public class BaseNameActiveEntity
{
    [Display(Name = "Name", Description = "Name")]
    [StringLength(100)]
    [RegularExpression("([a-zA-Z]+)", ErrorMessage = "Enter only alphabetical letters for Name")]
    public required string Name { get; set; }

    [Display(Name = "Active", Description = "Active")]
    public required bool IsActive { get; set; } = true;

    public override string? ToString() => Name;
}