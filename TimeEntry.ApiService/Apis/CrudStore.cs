namespace TimeEntry.ApiService.Apis;

/// <summary>
/// The five things every CRUD endpoint asks of a repository. GenericRepo and NameActiveRepo have them under the same names but share no interface (and
/// differ in what an update returns), so this wraps either one for <see cref="CrudApi{TEntity, TInput, TOutput}"/>.
/// </summary>
public sealed class CrudStore<T>(
    Func<int, Task<T?>> getById,
    Func<int, Task<bool>> exists,
    Func<T, Task<bool>> add,
    Func<int, T, Task<T?>> update,
    Func<string, int, Task<int>> delete) where T : class
{
    public Task<T?> GetByIdAsync(int id) => getById(id);

    public Task<bool> ExistsAsync(int id) => exists(id);

    /// <summary> False when the repository refused the row because another active row has that name. </summary>
    public Task<bool> AddAsync(T row) => add(row);

    /// <summary> The saved row, or null when the repository refused the change because another active row has that name. </summary>
    public Task<T?> UpdateAsync(int id, T row) => update(id, row);

    /// <summary> 0 deleted, -1 there was no such row, anything else: something still uses it. </summary>
    public Task<int> DeleteAsync(string table, int id) => delete(table, id);
}

public static class CrudStore
{
    public static CrudStore<T> For<T>(GenericRepo<T> repo) where T : BaseEntity =>
        new(repo.GetByIdAsync, repo.ExistsAsync, repo.AddAsync, async (id, row) => await repo.UpdateAsync(id, row), repo.DeleteAsync);

    public static CrudStore<T> For<T>(NameActiveRepo<T> repo) where T : BaseNameActiveEntity =>
        new(repo.GetByIdAsync, repo.ExistsAsync, repo.AddAsync, repo.UpdateAsync, repo.DeleteAsync);
}
