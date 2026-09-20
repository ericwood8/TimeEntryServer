namespace TimeEntry.Common.Repositories;

public class E_RequestExpenseDetailRepo : GenericRepo<E_RequestExpenseDetail>
{
    public E_RequestExpenseDetailRepo(TimeEntryContext context) : base(context)
    {
    }
}
