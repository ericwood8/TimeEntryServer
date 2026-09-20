namespace TimeEntry.Common.Repositories;

public class ResponseRepo : GenericRepo<Response>
{
    public ResponseRepo(TimeEntryContext context) : base(context)
    {
    }
}
