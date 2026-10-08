using System.Linq.Expressions;

namespace TimeEntry.Common.Repositories;

public class E_RequestExpenseSheetRepo : GenericRepo<E_RequestExpenseSheet>
{
    public E_RequestExpenseSheetRepo(TimeEntryContext context) : base(context)
    {
    }

    public async Task<List<E_RequestExpenseSheet>> GetAllIncludingDetails(Expression<Func<E_RequestExpenseSheet, bool>>? where = null)
    {
        IQueryable<E_RequestExpenseSheet> rows = where == null ? _dbSet : _dbSet.Where(where);
        return await rows
            .Include(s => s.ExpenseDetails)
            .ToListAsync();
    }
}