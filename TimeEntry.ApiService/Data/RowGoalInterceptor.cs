using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace TimeEntry.ApiService.Data;

/// <summary>
/// Adds OPTION (USE HINT('DISABLE_OPTIMIZER_ROWGOAL')) to a query tagged with <see cref="Tag"/> (<c>.TagWith(RowGoalInterceptor.Tag)</c>).
/// A page of "rows that match this text, sorted by the employee's name" makes SQL Server assume the first 10 matches turn up quickly, so it walks
/// the employees in name order looking up each one's rows; when few rows match that is a scan of the whole table (4 seconds for 100,000 time
/// sheets, 17 seconds when nothing matches). Without the row goal it picks the plan for the whole result (80 milliseconds).
/// </summary>
public sealed class RowGoalInterceptor : DbCommandInterceptor
{
    public const string Tag = "DisableRowGoal";

    private const string Hint = "OPTION (USE HINT('DISABLE_OPTIMIZER_ROWGOAL'))";

    private static void AddHint(DbCommand command)
    {
        if (command.CommandText.StartsWith("-- " + Tag, StringComparison.Ordinal) && !command.CommandText.Contains(Hint, StringComparison.Ordinal))
            command.CommandText += Environment.NewLine + Hint;
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        AddHint(command);
        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        AddHint(command);
        return ValueTask.FromResult(result);
    }
}
