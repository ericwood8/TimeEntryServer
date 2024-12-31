using System.Linq.Expressions;

namespace TimeEntry.Common.Repositories;

public interface IGenericRepo<T> where T : BaseEntity
{
    //--------- GET ROW -----------------
    public T GetById(int id);
    public Task<T> GetByIdAsync(int id);
    public T Get(Expression<Func<T, bool>> predicate);
    public Task<T?> GetAsync(Expression<Func<T, bool>> predicate);

    //--------- GET ROWS -----------------
    public Task<List<T>> GetAll();
    //public Task<List<T>> GetAllAsync();
    public Task<List<T>> GetAllOrderByDescending(Expression<Func<T, DateTime>> predicate);

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