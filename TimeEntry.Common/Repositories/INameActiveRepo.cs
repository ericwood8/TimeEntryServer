using System.Linq.Expressions;

namespace TimeEntry.Common.Repositories;

public interface INameActiveRepo<T> where T : BaseNameActiveEntity
{
    //--------- GET ROW -----------------
    public T GetById(int id);
    public Task<T> GetByIdAsync(int id);
    public T Get(Expression<Func<T, bool>> predicate);
    public Task<T?> GetAsync(Expression<Func<T, bool>> predicate);

    //--------- GET ROWS -----------------
    public Task<List<T>> GetAllActive();
    public Task<List<T>> GetByName(string name);

    //public Task<List<T>> GetAllAsync();

    //--------- Insert -----------------
    public Task<bool> AddAsync(T newRow);
    public void AddRange(IEnumerable<T> newRows);

    // ------- Update -------------------
    public Task<T> UpdateAsync(int id, T rowToUpdate);

    // ------- Delete -------------------
    public Task<int> DeleteAsync(string deleteFromTable, int deleteId);
    public void RemoveRange(List<T> rowsToDelete);

    // ------- Special - such as count -------------------
    public int Count();
    public Task<int> CountAsync();
}
