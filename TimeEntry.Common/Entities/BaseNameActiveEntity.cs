namespace TimeEntry.Common.Entities;

/// <summary>  BaseEntity + Name and IsActive columns </summary>
public class BaseNameActiveEntity : IHasName
{
    [Display(Name = "Name", Description = "Name")]
    [StringLength(100)]
    [RegularExpression(@"^[\p{L}\p{N}][\p{L}\p{N} '\-]*$", ErrorMessage = "Name may have letters, digits, spaces, apostrophes and hyphens, and must not start with a space")]
    public required string Name { get; set; }

    [Display(Name = "Active", Description = "Active")]
    public required bool IsActive { get; set; } = true;

    public override string? ToString() => Name;
}