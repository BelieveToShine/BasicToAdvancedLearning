using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FullStackBank.Api.Data;

// Everything about "which database do we talk to" lives here.
//
// The connection string comes ONLY from the BTAL_SQLSERVER environment variable (never from source or
// appsettings.json, so no password can be committed). Same variable the console lessons use, e.g.
//   Server=localhost,1433;User Id=sa;Password=<yours>;TrustServerCertificate=True;Initial Catalog=BasicToAdvancedLearning
// This API uses its OWN database: "<Initial Catalog>_FullStackBank", so it never touches a lesson's scratch database.
public static class BankDatabase
{
    public const string EnvironmentVariable = "BTAL_SQLSERVER";

    private static string? _connectionString;

    public static string ConnectionString => _connectionString ??= Build();

    private static string Build()
    {
        string? raw = Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException(
                $"Set the {EnvironmentVariable} environment variable to a SQL Server connection string (see FullStackBank/README.md).");
        }

        var builder = new SqlConnectionStringBuilder(raw);
        string baseName = string.IsNullOrEmpty(builder.InitialCatalog) || builder.InitialCatalog.Equals("master", StringComparison.OrdinalIgnoreCase)
            ? "BasicToAdvancedLearning"
            : builder.InitialCatalog;
        builder.InitialCatalog = baseName + "_FullStackBank";
        return builder.ConnectionString;
    }

    // Development convenience: create the database if it is missing (connects to "master" to do it).
    // A production database is created by whoever runs the server (a DBA / deployment script), with a login
    // that is allowed to do so; the API's own login should not have that right.
    public static async Task EnsureDatabaseExistsAsync()
    {
        var builder = new SqlConnectionStringBuilder(ConnectionString);
        string database = builder.InitialCatalog;
        builder.InitialCatalog = "master";

        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        // The name is passed as a parameter; CREATE DATABASE cannot take a parameter, so the quoted name is built inside T-SQL.
        command.CommandText = "IF DB_ID(@name) IS NULL EXEC('CREATE DATABASE ' + @quoted)";
        command.Parameters.AddWithValue("@name", database);
        command.Parameters.AddWithValue("@quoted", "[" + database.Replace("]", "]]") + "]");
        await command.ExecuteNonQueryAsync();
    }

    // Three customers, one account each, each with an opening deposit so balance = sum of its transactions.
    // Runs only when the Customers table is empty, so restarting the API never duplicates the data.
    public static async Task SeedIfEmptyAsync(BankDbContext db)
    {
        if (await db.Customers.AnyAsync())
        {
            return;
        }

        DateTime now = DateTime.UtcNow;
        var seed = new (string Name, string Email, string Number, AccountType Type, decimal Opening)[]
        {
            ("Asha Rao", "asha.rao@example.com", "ACC-1001", AccountType.Savings, 5000.00m),
            ("Ben Carter", "ben.carter@example.com", "ACC-1002", AccountType.Current, 1200.50m),
            ("Chen Li", "chen.li@example.com", "ACC-1003", AccountType.Savings, 300.00m),
        };

        foreach (var s in seed)
        {
            var customer = new Customer { FullName = s.Name, Email = s.Email, CreatedAt = now };
            var account = new Account { Customer = customer, AccountNumber = s.Number, AccountType = s.Type, Balance = s.Opening };
            account.Transactions.Add(new BankTransaction { Kind = TransactionKind.Deposit, Amount = s.Opening, CreatedAt = now });
            db.Accounts.Add(account);
        }

        await db.SaveChangesAsync();
    }
}
