using System.Linq.Expressions;
using TimeEntry.Common.Models;

namespace TimeEntry.Common.Repositories;

/// <summary> Generic Repository + GetByName() + IsDup() + different GetAll() </summary>
public class NameActiveRepo<T> : INameActiveRepo<T> where T : BaseNameActiveEntity
{
    protected readonly TimeEntryContext _context;
    protected readonly DbSet<T> _dbSet;

    public NameActiveRepo(TimeEntryContext context)
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
        newRow.Name = newRow.Name.Trim();
        if (await IsDupOnCreateAsync(newRow.Name))
        {
            return false;
        }

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
    /// <summary> Returns the saved row, or null when another active row already has that name. </summary>
    public async Task<T?> UpdateAsync(int id, T rowToUpdate)
    {
        rowToUpdate.Name = rowToUpdate.Name.Trim();
        if (rowToUpdate.IsActive && await IsDupOnUpdateAsync(id, rowToUpdate.Name))
        {
            return null;
        }

        _context.Entry(rowToUpdate).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return await GetByIdAsync(id) ?? throw new InvalidOperationException($"Entity of type {typeof(T).Name} with id {id} not found."); // refetch changed row
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

    // ------- Special - such as count or IsDup() -------------------
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

    // Name rule (create and update alike): an active row with the same name, ignoring surrounding spaces; upper/lower case follows
    // the database collation (case-insensitive on the default SQL Server one).
    private string KeyName => _context.Model.FindEntityType(typeof(T))!.FindPrimaryKey()!.Properties[0].Name;

    public async Task<bool> IsDupOnCreateAsync(string newName)
    {
        string name = newName.Trim();
        return await _dbSet.AsNoTracking().AnyAsync(d => d.Name == name && d.IsActive);
    }

    /// <summary> True when a different active row already has that name. The row being renamed never counts against itself. </summary>
    public async Task<bool> IsDupOnUpdateAsync(int id, string newName)
    {
        string name = newName.Trim();
        string keyName = KeyName;
        return await _dbSet.AsNoTracking().AnyAsync(d => d.Name == name && d.IsActive && EF.Property<int>(d, keyName) != id);
    }
    #endregion
}
