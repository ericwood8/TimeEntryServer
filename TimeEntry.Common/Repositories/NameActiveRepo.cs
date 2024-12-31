using System.Linq.Expressions;
using TimeEntry.Common.Models;

namespace TimeEntry.Common.Repositories;

/// <summary> Generic Repository + GetByName() + IsDup() + different GetAll() </summary>
public class NameActiveRepo<T> : INameActiveRepo<T>, IDisposable where T : BaseNameActiveEntity
{
    protected readonly TimeEntryContext _context;
    protected readonly DbSet<T> _dbSet;

    private bool _disposed = false;

    public NameActiveRepo(TimeEntryContext context)
    {
        _context = context;
        _dbSet = _context.Set<T>();
    }

    //--------- GET ROW -----------------

    public T GetById(int id)
    {
        return _dbSet.Find(id)!;
    }

    public async Task<T> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public T Get(Expression<Func<T, bool>> predicate)
    {
        return _dbSet.FirstOrDefault(predicate)!;
    }

    public async Task<T?> GetAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate);
    }

    //--------- GET ROWS -----------------

    public List<T> GetList(Expression<Func<T, bool>> predicate)
    {
        return _dbSet.Where(predicate).ToList();
    }

    // **** special ****
    public async Task<List<T>> GetByName(string name)
    {
        return await _dbSet
            .Where(d => d.Name.StartsWith(name) && d.IsActive)
            .ToListAsync();
    }

    // **** special ****
    public async Task<List<T>> GetAllActive()
    {
        return await _dbSet
            .Where(d => d.IsActive) // only fetch active
            .OrderBy(d => d.Name) // order by name
            .ToListAsync();
    }

    //--------- Insert -----------------

    public async Task<bool> AddAsync(T newRow)
    {
        if (IsDupOnCreate(newRow.Name))
        {
            return false;
        }

        _dbSet.Add(newRow);
        await _context.SaveChangesAsync();
        return true;
    }

    public void AddRange(IEnumerable<T> newRows)
    {
        _dbSet.AddRange(newRows);
        _context.SaveChangesAsync();
    }

    // ------- Update -------------------
    public async Task<T> UpdateAsync(int id, T rowToUpdate)
    {
        //if (IsDupOnUpdate(id, rowToUpdate.Name))
        //{
        //    return default(T); 
        //}

        _context.Entry(rowToUpdate).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return await GetByIdAsync(id); // refetch changed row
    }

    // ------- Delete -------------------
    public async Task<int> DeleteAsync(string deleteFromTable, int deleteId)
    {
        var rowToDelete = GetById(deleteId);
        if (rowToDelete == null)
            return -1;

        if (SpCanDeleteAsync(deleteFromTable, deleteId).Result.Count > 0)
            return -2;

        _dbSet.Remove(rowToDelete);
        await _context.SaveChangesAsync();
        return 0;
    }

    public void RemoveRange(List<T> rowsToDelete)
    {
        _dbSet.RemoveRange(rowsToDelete);
        _context.SaveChangesAsync();
    }

    // ------- Special - such as count or IsDup() -------------------
    public int Count()
    {
        return _dbSet.Count();
    }

    public async Task<int> CountAsync()
    {
        return await _dbSet.CountAsync();
    }


    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                //__context.Dispose();
            }
        }
        _disposed = true;
    }

    public void Dispose()
    {
        //Dispose(__context,true);
        GC.SuppressFinalize(this);
    }

    #region Privates
    /// <summary> Returns all the places that row is used in other tables. </summary>
    /// <param name="deleteFromTable">The table name</param>
    /// <param name="deleteId"> Id </param>
    /// <returns> List of rows in other tables that depend on this row. </returns>
    private async Task<List<DeleteTableResult>> SpCanDeleteAsync(string deleteFromTable, int deleteId)
    {
        return await _context.SpCanDeleteAsync(deleteFromTable, deleteId);
    }

    // **** special ****
    private bool IsDupOnCreate(string newName)
    {
        var uniqueRow = _dbSet.FirstOrDefault(d => d.Name.Equals(newName.Trim()) && d.IsActive);
        return uniqueRow != null;  // already exists 
    }

    // **** special ****
    private bool IsDupOnUpdate(int preChangeId, string newName)
    {
        // extra check on Update
        var preChangeRow = _dbSet.Find(preChangeId)!;
        if (preChangeRow.Name.Equals(newName, StringComparison.OrdinalIgnoreCase))
            return false; // don't check for dup if same name (because of course it will seem to be duplicate)

        var uniqueRow = _dbSet.FirstOrDefault(d => d.Name.Equals(newName.Trim()) && d.IsActive);
        return uniqueRow != null;  // already exists 
    }
    #endregion
}
