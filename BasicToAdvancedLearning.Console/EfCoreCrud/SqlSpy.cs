using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BasicToAdvancedLearning.EfCoreCrud;

// A tiny "spy" that EF Core calls just before it sends each SQL command to SQL Server. It lets the lesson
// print the REAL SQL EF generated and count how many commands (round trips) a piece of code caused.
// (An interceptor is EF Core's hook for this; the lesson only needs to know it sees every command.)
public class SqlSpy : DbCommandInterceptor
{
    // How many SQL commands EF has sent since the spy was created or last reset.
    public int Count { get; private set; }

    // When true, each command's text is printed as it goes out.
    public bool Print { get; set; } = true;

    public void Reset() => Count = 0;

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        Seen(command);
        return result;
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
    {
        Seen(command);
        return result;
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
    {
        Seen(command);
        return result;
    }

    private void Seen(DbCommand command)
    {
        Count++;
        if (!Print)
        {
            return;
        }

        Console.WriteLine($"  SQL #{Count}:");
        foreach (string line in command.CommandText.Split('\n'))
        {
            string trimmed = line.TrimEnd('\r');
            if (trimmed.Length > 0)
            {
                Console.WriteLine("    " + trimmed);
            }
        }

        foreach (DbParameter parameter in command.Parameters)
        {
            Console.WriteLine($"    -- {parameter.ParameterName} = {parameter.Value}");
        }
    }
}
