using System.Data;
using Microsoft.Data.SqlClient;
using BasicToAdvancedLearning.Interfaces;

namespace BasicToAdvancedLearning.AdoNet;

// Topic 13: ADO.NET basics — how a C# program talks to SQL Server. ADO.NET is the set of classes in
// System.Data / Microsoft.Data.SqlClient: a SqlConnection (the open line to the server), a SqlCommand
// (carries ONE piece of T-SQL down that line) and a SqlDataReader (walks the rows that come back).
// Topics 10-12 sent SQL through a small helper (SqlLab) so the lesson could stay about SQL; this topic
// opens the helper's box and uses the real classes directly. Each method resets the little bank
// (Customers + Accounts) first, so you can read any one method on its own.
// No async here: every call blocks until SQL Server answers (async/await comes later in the course).
// See ArchitectureDiagrams/AdoNet for the visual walkthrough.
public class AdoNetDemo
{
    // The connection string comes from configuration (the BTAL_SQLSERVER environment variable), never
    // from source code: it holds the server address and often a password. SqlLab.IsReady() has already
    // checked in Explain() that it is set, so the empty-string fallback is never reached in a normal run.
    private static string ConnectionString => SqlLab.ConnectionString ?? string.Empty;

    // The bank's two tables, as T-SQL. The first lines drop an older copy (child table first, because
    // Accounts points at Customers) so every run starts from the same empty state.
    // WARNING: this lesson DROPS and recreates dbo.Transactions, dbo.Accounts and dbo.Customers (in its own
    // scratch database, which SqlLab creates for it). Transactions is dropped only so the foreign-key order works.
    private const string CreateTablesSql = @"
        IF OBJECT_ID('dbo.Transactions', 'U') IS NOT NULL DROP TABLE dbo.Transactions;
        IF OBJECT_ID('dbo.Accounts', 'U') IS NOT NULL DROP TABLE dbo.Accounts;
        IF OBJECT_ID('dbo.Customers', 'U') IS NOT NULL DROP TABLE dbo.Customers;
        CREATE TABLE dbo.Customers (
            CustomerId INT IDENTITY(1,1) PRIMARY KEY,
            FullName   NVARCHAR(100) NOT NULL,
            Email      NVARCHAR(200) NOT NULL UNIQUE,
            CreatedAt  DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME());
        CREATE TABLE dbo.Accounts (
            AccountId     INT IDENTITY(1,1) PRIMARY KEY,
            CustomerId    INT NOT NULL REFERENCES dbo.Customers (CustomerId),
            AccountNumber NVARCHAR(20) NOT NULL UNIQUE,
            AccountType   NVARCHAR(20) NOT NULL CHECK (AccountType IN ('Savings', 'Current')),
            Balance       DECIMAL(18,2) NOT NULL DEFAULT 0);";

    private const string InsertCustomersSql =
        "INSERT INTO dbo.Customers (FullName, Email) VALUES " +
        "('Asha Rao', 'asha@example.com'), ('Ben Carter', 'ben@example.com'), ('Chen Li', 'chen@example.com')";

    // A connection is the open line to the server; a command carries one piece of SQL down that line.
    public void ExplainConnectionAndCommand()
    {
        PrintHeading("1. Connection and command — opening the line, sending SQL");

        // Note: this lesson DROPS and recreates Customers and Accounts (and drops Transactions, only for FK order)
        // in its own scratch database, which SqlLab creates for it.

        string connectionString = ConnectionString;

        // The `using` block closes (disposes) the connection at its closing brace, even if an exception
        // is thrown inside — the same idea as the stream in Topic 8. By default SqlClient keeps a pool of
        // open connections, so disposing normally hands the line back to the pool for reuse.
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            // Creating the object does not contact the server; Open() does.
            Console.WriteLine($"Before Open(): {connection.State}");
            connection.Open();
            Console.WriteLine($"After Open():  {connection.State}");

            using (SqlCommand create = connection.CreateCommand())
            {
                create.CommandText = CreateTablesSql;

                // ExecuteNonQuery runs SQL that returns no rows (CREATE, INSERT, UPDATE, DELETE). It returns
                // the number of rows changed; for CREATE/DROP there is no such count, so it returns -1.
                int created = create.ExecuteNonQuery();
                Console.WriteLine($"CREATE TABLE ran. ExecuteNonQuery returned {created}.");
            }

            using (SqlCommand insert = connection.CreateCommand())
            {
                insert.CommandText = InsertCustomersSql;
                int inserted = insert.ExecuteNonQuery();
                Console.WriteLine($"INSERT ran. ExecuteNonQuery returned {inserted} (rows added).");
            }

            using (SqlCommand count = connection.CreateCommand())
            {
                count.CommandText = "SELECT COUNT(*) FROM dbo.Customers";

                // ExecuteScalar runs SQL that returns ONE value (first column of the first row). It is typed
                // as object, so we convert it; COUNT(*) is an int in SQL Server.
                int customers = (int)count.ExecuteScalar()!;
                Console.WriteLine($"Rows now in Customers: {customers}");
            }
        }
    }

    // Parameters send values separately from the SQL text, so a value is not run as SQL
    // (parameters protect VALUES, not table or column names).
    public void ExplainParameters()
    {
        PrintHeading("2. Parameters — values travel separately from the SQL");
        ResetBank();

        using (SqlConnection connection = new SqlConnection(ConnectionString))
        {
            connection.Open();

            // @name and @email are placeholders. Two ways to fill them in:
            //  - AddWithValue(name, value): SqlClient GUESSES the SQL type from the C# value. It is fine for a quick
            //    demo, but the guessed type and size can differ from the column (e.g. a decimal's precision/scale
            //    come out as 0/0), so real code often prefers Parameters.Add with an explicit type.
            //  - Parameters.Add(name, SqlDbType, size).Value = ...: YOU state the type and length.
            using (SqlCommand insert = connection.CreateCommand())
            {
                insert.CommandText = "INSERT INTO dbo.Customers (FullName, Email) VALUES (@name, @email)";
                insert.Parameters.AddWithValue("@name", "Dana O'Neil");
                insert.Parameters.Add("@email", SqlDbType.NVarChar, 200).Value = "dana@example.com";

                int rowsAffected = insert.ExecuteNonQuery();
                Console.WriteLine($"Inserted a customer whose name has an apostrophe: {rowsAffected} row affected.");
            }

            // The rest of this method is a DEMONSTRATION OF WHAT NOT TO DO, run on two throw-away scratch
            // tables so nothing real can be harmed. ScratchNames stores names; ScratchVictim just has to survive.
            Execute(connection,
                "IF OBJECT_ID('dbo.ScratchNames', 'U') IS NOT NULL DROP TABLE dbo.ScratchNames; " +
                "IF OBJECT_ID('dbo.ScratchVictim', 'U') IS NOT NULL DROP TABLE dbo.ScratchVictim; " +
                "CREATE TABLE dbo.ScratchNames (Name NVARCHAR(100) NOT NULL); CREATE TABLE dbo.ScratchVictim (Id INT NOT NULL);");

            // Text typed by a user (or a web form). It LOOKS like a name followed by more SQL.
            string attackerInput = "Ravi'); DROP TABLE dbo.ScratchVictim; --";

            // UNSAFE: gluing the text into the SQL. The ' closes the quote, ) finishes the INSERT, then
            // ; starts a NEW statement (DROP TABLE), and -- turns the rest of the line into a comment.
            // SQL Server allows several statements in one command text, so it runs all of them.
            string unsafeSql = "INSERT INTO dbo.ScratchNames (Name) VALUES ('" + attackerInput + "')";
            Console.WriteLine($"Glued SQL: {unsafeSql}");
            Console.WriteLine($"ScratchVictim exists before: {TableExists(connection, "ScratchVictim")}");
            using (SqlCommand unsafeCommand = connection.CreateCommand())
            {
                unsafeCommand.CommandText = unsafeSql;
                unsafeCommand.ExecuteNonQuery();
            }
            Console.WriteLine($"ScratchVictim exists after the glued INSERT: {TableExists(connection, "ScratchVictim")}  <-- SQL injection");

            // SAFE: the same text sent as a parameter. SQL Server treats it purely as a value, so it is
            // stored as ordinary text and the DROP TABLE is never run.
            Execute(connection, "CREATE TABLE dbo.ScratchVictim (Id INT NOT NULL);");
            using (SqlCommand safeCommand = connection.CreateCommand())
            {
                safeCommand.CommandText = "INSERT INTO dbo.ScratchNames (Name) VALUES (@name)";
                safeCommand.Parameters.AddWithValue("@name", attackerInput); // demo shortcut; see the note above
                safeCommand.ExecuteNonQuery();

                // Same command object, new text: the @name parameter is still attached to it.
                safeCommand.CommandText = "SELECT TOP (1) Name FROM dbo.ScratchNames WHERE Name = @name";
                string storedName = (string)safeCommand.ExecuteScalar()!;
                Console.WriteLine($"With a parameter, ScratchVictim exists: {TableExists(connection, "ScratchVictim")}");
                Console.WriteLine($"The stored name is just text: {storedName}");
            }

            Execute(connection, "DROP TABLE dbo.ScratchNames; DROP TABLE dbo.ScratchVictim;");
        }
    }

    // ExecuteReader walks the result rows one at a time, like a finger moving down a printed table.
    public void ExplainReadingRows()
    {
        PrintHeading("3. Reading rows — a reader walks the result");
        ResetBank();

        using (SqlConnection connection = new SqlConnection(ConnectionString))
        {
            connection.Open();

            using (SqlCommand command = connection.CreateCommand())
            {
                // LEFT JOIN keeps every customer, even one with no account: that customer's account
                // columns come back as NULL (this is why we must check IsDBNull below).
                command.CommandText =
                    "SELECT c.FullName, a.AccountNumber, a.Balance " +
                    "FROM dbo.Customers c LEFT JOIN dbo.Accounts a ON a.CustomerId = c.CustomerId " +
                    "ORDER BY c.CustomerId, a.AccountId";

                // The reader holds the open result. With default settings a connection serves ONE open
                // reader at a time (a second command on it throws InvalidOperationException), so `using`
                // closes the reader at its closing brace to free the connection again.
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    // Column positions are looked up ONCE by name, then reused for every row.
                    int nameColumn = reader.GetOrdinal("FullName");
                    int numberColumn = reader.GetOrdinal("AccountNumber");
                    int balanceColumn = reader.GetOrdinal("Balance");

                    // Read() moves to the next row and returns false when there are no more rows.
                    while (reader.Read())
                    {
                        string fullName = reader.GetString(nameColumn);

                        // A NULL cannot be turned into a string or decimal: GetString/GetDecimal would throw SqlNullValueException.
                        if (reader.IsDBNull(numberColumn))
                        {
                            Console.WriteLine($"  {fullName}: no account yet (the account columns are NULL)");
                            continue;
                        }

                        string accountNumber = reader.GetString(numberColumn);
                        decimal balance = reader.GetDecimal(balanceColumn); // DECIMAL(18,2) arrives as a C# decimal
                        Console.WriteLine($"  {fullName}: {accountNumber} holds {balance}");
                    }
                }
            }

            using (SqlCommand sum = connection.CreateCommand())
            {
                sum.CommandText = "SELECT SUM(Balance) FROM dbo.Accounts";
                decimal total = (decimal)sum.ExecuteScalar()!;
                Console.WriteLine($"Total across all accounts: {total}");
            }
        }
    }

    // A transaction makes several changes all-or-nothing: either every step is kept, or none is.
    public void ExplainTransactions()
    {
        PrintHeading("4. Transactions — all or nothing");
        ResetBank();

        // Two transfers of 200.00 out of ACC-1001: the first to a real account, the second to one that
        // does not exist, so its second step (the credit) fails on purpose.
        string[] destinations = { "ACC-2001", "ACC-9999" };
        const decimal Amount = 200.00m;

        using (SqlConnection connection = new SqlConnection(ConnectionString))
        {
            connection.Open();
            Console.WriteLine($"Before:  {DescribeBalances(connection, null)}");

            foreach (string toAccount in destinations)
            {
                Console.WriteLine($"Transfer {Amount} from ACC-1001 to {toAccount}:");

                // BeginTransaction starts the all-or-nothing group; `using` disposes it at the closing brace.
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        int debited = ChangeBalance(connection, transaction, "ACC-1001", -Amount);
                        Console.WriteLine($"  debit  ACC-1001: UPDATE changed {debited} row(s)");

                        int credited = ChangeBalance(connection, transaction, toAccount, +Amount);
                        Console.WriteLine($"  credit {toAccount}: UPDATE changed {credited} row(s)");

                        if (credited == 0)
                        {
                            // An UPDATE that matches nothing is not an SQL error, so we raise one ourselves.
                            throw new InvalidOperationException($"Account {toAccount} does not exist.");
                        }

                        transaction.Commit(); // make both changes permanent
                        Console.WriteLine("  Commit(): both steps kept.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  FAILED: {ex.Message}");
                        Console.WriteLine($"  Inside the transaction: {DescribeBalances(connection, transaction)}");
                        transaction.Rollback(); // undo EVERYTHING since BeginTransaction
                        Console.WriteLine("  Rollback(): the debit is undone too.");
                    }
                }

                Console.WriteLine($"  After:   {DescribeBalances(connection, null)}");
            }
        }
    }

    // WARNING: DROPS and recreates dbo.Transactions, dbo.Accounts and dbo.Customers in this lesson's own scratch
    // database (created by SqlLab). Drops Customers + Accounts and seeds Asha (two accounts), Ben (one) and Chen (none),
    // so each lesson method starts from the same data. It is the same SqlConnection/SqlCommand code as method 1.
    private static void ResetBank()
    {
        using (SqlConnection connection = new SqlConnection(ConnectionString))
        {
            connection.Open();
            Execute(connection, CreateTablesSql);
            Execute(connection, InsertCustomersSql);
            Execute(connection,
                "INSERT INTO dbo.Accounts (CustomerId, AccountNumber, AccountType, Balance) VALUES " +
                "(1, 'ACC-1001', 'Savings', 1000.00), (1, 'ACC-1002', 'Current', 250.50), (2, 'ACC-2001', 'Current', 500.00)");
        }
    }

    // Runs one SQL text that returns no rows and gives back the row count.
    private static int Execute(SqlConnection connection, string sql)
    {
        using (SqlCommand command = connection.CreateCommand())
        {
            command.CommandText = sql;
            return command.ExecuteNonQuery();
        }
    }

    // ExecuteScalar on a catalog query: is there a table with this name in the database?
    private static bool TableExists(SqlConnection connection, string tableName)
    {
        using (SqlCommand command = connection.CreateCommand())
        {
            command.CommandText = "SELECT COUNT(*) FROM sys.tables WHERE name = @table";
            command.Parameters.AddWithValue("@table", tableName); // demo shortcut; real code often states the type
            return (int)command.ExecuteScalar()! > 0;
        }
    }

    // Adds (or, if negative, subtracts) an amount on one account as part of the given transaction.
    // Returns how many rows the UPDATE changed: 1 for a real account, 0 when the number matches nothing.
    private static int ChangeBalance(SqlConnection connection, SqlTransaction transaction, string accountNumber, decimal delta)
    {
        using (SqlCommand command = connection.CreateCommand())
        {
            // A command must be told which transaction it belongs to. While the connection has a
            // transaction open, SqlClient throws InvalidOperationException for a command that was not given one.
            command.Transaction = transaction;
            command.CommandText = "UPDATE dbo.Accounts SET Balance = Balance + @delta WHERE AccountNumber = @number";
            command.Parameters.Add("@delta", SqlDbType.Decimal).Value = delta;
            command.Parameters.Add("@number", SqlDbType.NVarChar, 20).Value = accountNumber;
            return command.ExecuteNonQuery();
        }
    }

    // Returns "ACC-1001 1000.00, ACC-1002 250.50, ..." so a lesson can print every balance on one line.
    // `transaction` is null when none is open on the connection; otherwise the read joins that transaction.
    private static string DescribeBalances(SqlConnection connection, SqlTransaction? transaction)
    {
        List<string> parts = new List<string>();

        using (SqlCommand command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "SELECT AccountNumber, Balance FROM dbo.Accounts ORDER BY AccountId";

            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    parts.Add($"{reader.GetString(0)} {reader.GetDecimal(1)}");
                }
            }
        }

        return string.Join(", ", parts);
    }

    private static void PrintHeading(string title)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', title.Length));
        Console.WriteLine(title);
        Console.WriteLine(new string('=', title.Length));
    }
}
