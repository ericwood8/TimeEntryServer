namespace TimeEntry.Common.Repositories;

public class RestrictLeaveRepo : GenericRepo<RestrictLeave>
{
    public RestrictLeaveRepo(TimeEntryContext context) : base(context)
    {
    }
}
