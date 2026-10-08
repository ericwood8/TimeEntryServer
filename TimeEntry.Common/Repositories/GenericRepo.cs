using System.Linq.Expressions;
using TimeEntry.Common.Models;

namespace TimeEntry.Common.Repositories;

/// <summary> Repository for any generic table </summary>
public class GenericRepo<T> : IGenericRepo<T> where T : BaseEntity
{
    protected readonly TimeEntryContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericRepo(TimeEntryContext context)
    {
        _context = context;
        _dbSet = _context.Set<T>();
    }

    //--------- GET ROW -----------------

    /// <summary> Returns the row, or null when there is no row with that id. </summary>
    public async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    /// <summary> True when a row with that id exists. Does not track the row, so a caller can still attach its own copy afterwards. </summary>
    public async Task<bool> ExistsAsync(int id)
    {
        string keyName = _context.Model.FindEntityType(typeof(T))!.FindPrimaryKey()!.Properties[0].Name;
        return await _dbSet.AsNoTracking().AnyAsync(e => EF.Property<int>(e, keyName) == id);
    }

    public async Task<T?> GetAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate);
    }

    //--------- GET ROWS -----------------

    public async Task<List<T>> GetListAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    public async Task<List<T>> GetAll()
    {
        return await _dbSet.ToListAsync();
    }

    /// <summary> All rows, newest first; <paramref name="where"/> narrows them when given. </summary>
    public async Task<List<T>> GetAllOrderByDescending(Expression<Func<T, DateTime>> predicate, Expression<Func<T, bool>>? where = null)
    {
        IQueryable<T> rows = where == null ? _dbSet : _dbSet.Where(where);
        return await rows.OrderByDescending(predicate).ToListAsync();
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

    public async Task AddRangeAsync(IEnumerable<T> newRows)
    {
        _dbSet.AddRange(newRows);
        await _context.SaveChangesAsync();
    }

    // ------- Update -------------------
    public async Task<T> UpdateAsync(int id, T rowToUpdate)
    {
        // NOTE - Do not worry about duplicate names since they should be part of NameActiveRepo, not here.

        _context.Entry(rowToUpdate).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return await GetByIdAsync(id) ?? throw new InvalidOperationException($"Entity with id {id} not found."); // refetch changed row
    }

    // ------- Delete -------------------
    public async Task<int> DeleteAsync(string deleteFromTable, int deleteId)
    {
        var rowToDelete = await GetByIdAsync(deleteId);
        if (rowToDelete == null)
            return -1;

        if ((await SpCanDeleteAsync(deleteFromTable, deleteId)).Count > 0)
            return -2;

        _dbSet.Remove(rowToDelete);
        await _context.SaveChangesAsync();
        return 0;
    }

    public async Task RemoveRangeAsync(List<T> rowsToDelete)
    {
        _dbSet.RemoveRange(rowsToDelete);
        await _context.SaveChangesAsync();
    }

    // ------- Special - such as count -------------------
    public async Task<int> CountAsync()
    {
        return await _dbSet.CountAsync();
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
