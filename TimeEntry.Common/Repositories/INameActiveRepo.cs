using System.Linq.Expressions;

namespace TimeEntry.Common.Repositories;

public interface INameActiveRepo<T> where T : BaseNameActiveEntity
{
    //--------- GET ROW -----------------
    public Task<T?> GetByIdAsync(int id);
    public Task<bool> ExistsAsync(int id);
    public Task<T?> GetAsync(Expression<Func<T, bool>> predicate);

    //--------- GET ROWS -----------------
    public Task<List<T>> GetAllActive();
    public Task<List<T>> GetByName(string name);
    public Task<List<T>> GetListAsync(Expression<Func<T, bool>> predicate);

    //public Task<List<T>> GetAllAsync();

    //--------- Insert -----------------
    public Task<bool> AddAsync(T newRow);
    public Task AddRangeAsync(IEnumerable<T> newRows);

    // ------- Update -------------------
    public Task<T?> UpdateAsync(int id, T rowToUpdate);

    // ------- Delete -------------------
    public Task<int> DeleteAsync(string deleteFromTable, int deleteId);
    public Task RemoveRangeAsync(List<T> rowsToDelete);

    // ------- Name rules -------------------
    public Task<bool> IsDupOnCreateAsync(string newName);
    public Task<bool> IsDupOnUpdateAsync(int id, string newName);

    // ------- Special - such as count -------------------
    public Task<int> CountAsync();
}
