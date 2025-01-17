namespace TimeEntry.Common.Repositories;

public class TimeEntryUserRepo : NameActiveRepo<TimeEntryUser>
{
    public TimeEntryUserRepo(TimeEntryContext context) : base(context)
    {
    }
}