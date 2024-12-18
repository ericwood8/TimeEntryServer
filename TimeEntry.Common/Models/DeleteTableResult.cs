namespace TimeEntry.Common.Models;

public class DeleteTableResult
{
    public string TableName { get; set; }
    public string ColName { get; set; }
    public string KeyName { get; set; }
    public int NumTimes { get; set; }
}