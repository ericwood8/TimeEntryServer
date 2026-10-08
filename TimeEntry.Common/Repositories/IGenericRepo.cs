using System.Linq.Expressions;

namespace TimeEntry.Common.Repositories;

public interface IGenericRepo<T> where T : BaseEntity
{
    //--------- GET ROW -----------------
    public Task<T?> GetByIdAsync(int id);
    public Task<bool> ExistsAsync(int id);
    public Task<T?> GetAsync(Expression<Func<T, bool>> predicate);

    //--------- GET ROWS -----------------
    public Task<List<T>> GetAll();
    public Task<List<T>> GetListAsync(Expression<Func<T, bool>> predicate);
    //public Task<List<T>> GetAllAsync();
    public Task<List<T>> GetAllOrderByDescending(Expression<Func<T, DateTime>> predicate, Expression<Func<T, bool>>? where = null);

    //--------- Insert -----------------
    public Task<bool> AddAsync(T newRow);
    public Task AddRangeAsync(IEnumerable<T> newRows);

    // ------- Update -------------------
    public Task<T> UpdateAsync(int id, T rowToUpdate);

    // ------- Delete -------------------
    public Task<int> DeleteAsync(string deleteFromTable, int deleteId);
    public Task RemoveRangeAsync(List<T> rowsToDelete);

    // ------- Special - such as count -------------------
    public Task<int> CountAsync();
}