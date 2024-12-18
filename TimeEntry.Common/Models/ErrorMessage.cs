namespace TimeEntry.Common.Models;

public class ErrorMessage
{
    public required string Message { get; set; }
    public string? ExceptionMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}