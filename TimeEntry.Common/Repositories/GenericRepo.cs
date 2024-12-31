using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TimeEntry.Common.Context;
using TimeEntry.Common.Entities;
using TimeEntry.Common.Models;

namespace TimeEntry.Common.Repositories;

/// <summary> Repository for any generic table </summary>
public class GenericRepo<T> : IGenericRepo<T>, IDisposable where T : BaseEntity
{
    protected readonly TimeEntryContext _context;
    protected readonly DbSet<T> _dbSet;
    private bool _disposed = false;

    public GenericRepo(TimeEntryContext context)
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

    //public async Task<IEnumerable<T>> GetListAsync(Expression<Func<T, bool>> predicate)
    //{
    //    return await Task.Run(() => _context.Set<T>().Where<T>(predicate));
    //}

    public async Task<List<T>> GetAll()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<List<T>> GetAllOrderByDescending(Expression<Func<T, DateTime>> predicate)
    {
        return await _dbSet.OrderByDescending(predicate).ToListAsync();
    }


    //public async Task<List<T>> GetAllAsync()
    //{
    //    return await Task.Run(() => _context.Set<T>());
    //}


    //--------- Insert -----------------

    public async Task<bool> AddAsync(T newRow)
    {
        // NOTE - Do not worry about duplicate names since they should be part of NameActiveRepo, not here.

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
        // NOTE - Do not worry about duplicate names since they should be part of NameActiveRepo, not here.

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

    // ------- Special - such as count -------------------
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
                _context.Dispose();
            }
        }
        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
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
    #endregion
}
