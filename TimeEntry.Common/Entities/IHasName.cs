namespace TimeEntry.Common.Entities;

/// <summary> A row with a name the API trims and checks the same way everywhere (E04). </summary>
public interface IHasName
{
    string Name { get; set; }
}
