namespace TimeEntry.Common.Repositories;

public class E_RequestExpenseSheetRepo : GenericRepo<E_RequestExpenseSheet>
{
    public E_RequestExpenseSheetRepo(TimeEntryContext context) : base(context)
    {
    }

    public async Task<List<E_RequestExpenseSheet>> GetAllIncludingDetails()
    {
        return await _dbSet
            .Include(s => s.ExpenseDetails)
            .ToListAsync();
    }
}