using System.Data;
using Microsoft.Data.SqlClient;

namespace BasicToAdvancedLearning.Interfaces;

// Shared plumbing for the SQL Server topics (10 and later). This folder holds shared code, not a
// lesson. SQL Server is the only database engine in this repo (docs/decisions.md D8).
//
// The connection string is NEVER in source. Set it in an environment variable before running:
//   BTAL_SQLSERVER = Server=localhost,1433;User Id=sa;Password=<yours>;TrustServerCertificate=True;Initial Catalog=BasicToAdvancedLearning
// Every lesson gets its OWN scratch database, named "<Initial Catalog>_<TopicFolder>" (for example
// BasicToAdvancedLearning_SqlCrud), created for you if it does not exist yet. So one lesson never
// touches another lesson's tables, and a lesson may drop and recreate its own tables (or even its
// whole database) freely. Only use a server where creating scratch databases is fine.
//
// Topics 10-12 are SQL lessons: they send T-SQL text through Run / Show so the lesson stays about
// SQL. How C# really talks to SQL Server (SqlConnection, SqlCommand, parameters) is Topic 13.
internal static class SqlLab
{
    public const string EnvironmentVariable = "BTAL_SQLSERVER";

    private static string? _lessonConnectionString;

    // The connection string the CURRENT lesson should use: the variable's value with the database name
    // changed to this lesson's own scratch database (set by IsReady). null when the variable is not set.
    public static string? ConnectionString =>
        _lessonConnectionString ?? Environment.GetEnvironmentVariable(EnvironmentVariable);

    // The lesson's folder name, taken from the file that called IsReady (the compiler fills the argument in).
    private static string TopicOf(string callerFile) =>
        new DirectoryInfo(Path.GetDirectoryName(callerFile) ?? ".").Name;

    // Call first in Explain(). Returns false (after printing why) when the lesson cannot run, so a
    // missing server is a friendly message instead of a crash.
    public static bool IsReady([System.Runtime.CompilerServices.CallerFilePath] string callerFile = "")
    {
        string? connectionString = Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.WriteLine($"This lesson talks to SQL Server. Set the {EnvironmentVariable} environment variable to a");
            Console.WriteLine("connection string for your server (see README.md), then run it again. Skipping.");
            return false;
        }

        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            string baseName = string.IsNullOrEmpty(builder.InitialCatalog) || builder.InitialCatalog.Equals("master", StringComparison.OrdinalIgnoreCase)
                ? "BasicToAdvancedLearning"
                : builder.InitialCatalog;
            builder.InitialCatalog = baseName + "_" + TopicOf(callerFile);
            connectionString = builder.ConnectionString;
            _lessonConnectionString = connectionString;
            EnsureDatabase(connectionString);
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            return true;
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"Could not connect to SQL Server: {ex.Message}");
            Console.WriteLine($"Check the {EnvironmentVariable} value and that the server is running. Skipping.");
            return false;
        }
    }

    // Creates the database named in the connection string if it is missing (connects to "master").
    public static void EnsureDatabase(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        string database = builder.InitialCatalog;
        if (string.IsNullOrEmpty(database) || database.Equals("master", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        builder.InitialCatalog = "master";
        using var connection = new SqlConnection(builder.ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "IF DB_ID(@name) IS NULL EXEC('CREATE DATABASE ' + @quoted)";
        command.Parameters.AddWithValue("@name", database);
        command.Parameters.AddWithValue("@quoted", "[" + database.Replace("]", "]]") + "]");
        command.ExecuteNonQuery();
    }

    // Runs one T-SQL batch that returns no rows (CREATE, INSERT, UPDATE, DELETE...). Returns the
    // number of rows affected as reported by SQL Server (-1 for statements that report none).
    public static int Run(string sql)
    {
        using var connection = new SqlConnection(ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return command.ExecuteNonQuery();
    }

    // Runs a SELECT and prints the rows as a small text grid, so the lesson shows what came back.
    public static void Show(string sql)
    {
        using var connection = new SqlConnection(ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        using SqlDataReader reader = command.ExecuteReader();

        var columns = new List<string>();
        for (int i = 0; i < reader.FieldCount; i++)
        {
            columns.Add(reader.GetName(i));
        }

        var rows = new List<string[]>();
        while (reader.Read())
        {
            var row = new string[reader.FieldCount];
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[i] = reader.IsDBNull(i) ? "NULL" : Convert.ToString(reader.GetValue(i), System.Globalization.CultureInfo.InvariantCulture) ?? "";
            }
            rows.Add(row);
        }

        var widths = new int[columns.Count];
        for (int i = 0; i < columns.Count; i++)
        {
            widths[i] = columns[i].Length;
            foreach (string[] row in rows)
            {
                widths[i] = Math.Max(widths[i], row[i].Length);
            }
        }

        Console.WriteLine("  " + string.Join(" | ", columns.Select((c, i) => c.PadRight(widths[i]))));
        Console.WriteLine("  " + string.Join("-+-", widths.Select(w => new string('-', w))));
        foreach (string[] row in rows)
        {
            Console.WriteLine("  " + string.Join(" | ", row.Select((v, i) => v.PadRight(widths[i]))));
        }
        Console.WriteLine($"  ({rows.Count} row{(rows.Count == 1 ? "" : "s")})");
    }

    // Runs a statement that is EXPECTED to be rejected by SQL Server and prints the error text, so a
    // lesson can show a constraint doing its job (for example a foreign key violation).
    public static void ShowError(string sql)
    {
        try
        {
            Run(sql);
            Console.WriteLine("  (no error - the statement succeeded)");
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"  SQL Server refused it (error {ex.Number}): {FirstLine(ex.Message)}");
        }
    }

    private static string FirstLine(string text) => text.Split('\n')[0].Trim();
}
