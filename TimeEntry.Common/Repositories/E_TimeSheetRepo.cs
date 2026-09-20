namespace TimeEntry.Common.Repositories;

public class E_TimeSheetRepo : GenericRepo<E_TimeSheet>
{
    public E_TimeSheetRepo(TimeEntryContext context) : base(context)
    {
    }
}
