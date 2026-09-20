namespace TimeEntry.Common.Repositories;

public class E_RequestRepo : GenericRepo<E_Request>
{
    public E_RequestRepo(TimeEntryContext context) : base(context)
    {
    }
}
