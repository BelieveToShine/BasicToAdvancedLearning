using BasicToAdvancedLearning.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BasicToAdvancedLearning.EfCoreBasics;

// Topic 14: EF Core basics - the MODEL side. Entity classes, the DbContext, the Fluent API and migrations.
// Actually reading and writing rows (queries, SaveChanges) is Topic 15; here the lesson only shapes and
// creates the tables. The running example is the bank: Customers and Accounts.
// EF Core is an "object-relational mapper" (ORM): it turns C# objects into table rows and back, and it writes
// the SQL for you. Topic 13 sent SQL text by hand; here you describe the tables as C# classes instead.
// See ArchitectureDiagrams/EfCoreBasics for the visual walkthrough.
public class EfCoreBasicsDemo
{
    // 1. Entities are plain C# classes: the objects that will become rows.
    public void ExplainModelClasses()
    {
        PrintHeading("1. Model classes - plain C# objects that EF Core maps to rows");

        // Two ordinary objects on the Heap. No database, no EF Core code yet.
        Customer asha = new Customer { FullName = "Asha Rao", Email = "asha@example.com" };
        Account savings = new Account { AccountNumber = "ACC-1001", AccountType = "Savings", Balance = 500m };

        // Link them through the navigation properties - the C# version of "a foreign key points at a row".
        asha.Accounts.Add(savings);
        savings.Customer = asha;

        Console.WriteLine($"{asha.FullName} has {asha.Accounts.Count} account(s): {asha.Accounts[0].AccountNumber}");
        Console.WriteLine($"The account's owner is the same object: {ReferenceEquals(savings.Customer, asha)}");

        // CustomerId is 0 because no row exists yet - the database hands out the number when the row is saved.
        Console.WriteLine($"CustomerId is still {asha.CustomerId}: no row has been saved");
    }

    // 2. The DbContext connects the classes to a database. Its DbSet<T> properties become tables.
    public void ExplainDbContextAndDbSet()
    {
        PrintHeading("2. DbContext and DbSet - what the conventions alone produce");

        // One DbContext instance per "unit of work" (one small job, such as handling one request), disposed
        // when the job ends. `using` calls Dispose() at the closing brace, which releases what the context holds.
        // A DbContext is not meant to be shared between threads or kept alive for the life of the program.
        // `context` is declared outside the using so the catch block below can show it is disposed.
        PlainBankContext context;
        using (context = new PlainBankContext())
        {
            // GenerateCreateScript only RETURNS the SQL EF Core would run to create the tables; it does not run it,
            // and it needs no database connection. "GO" is not T-SQL: it is the batch separator that tools such as
            // sqlcmd understand, so the script can be pasted into one of them.
            //
            // Read the script against the classes in BankModels.cs - these are the conventions at work:
            //  - Table names come from the DbSet property names (Customers, Accounts).
            //  - CustomerId / AccountId are the primary keys (named <Class>Id) and are IDENTITY, so the database numbers the rows.
            //  - `string` becomes nvarchar(max) NOT NULL: no length limit yet - method 3 fixes that.
            //  - decimal becomes decimal(18,2) (EF Core's fallback when nothing is configured; it logs a warning about that) and DateTime becomes datetime2.
            //  - Account.CustomerId is the foreign key and gets an index; because it cannot be null, the
            //    default delete behaviour is ON DELETE CASCADE (deleting a customer would delete their accounts).
            string script = context.Database.GenerateCreateScript();
            Console.WriteLine(script.TrimEnd());
        }

        // After Dispose the context cannot be used any more.
        try
        {
            context.Database.GenerateCreateScript();
        }
        catch (ObjectDisposedException)
        {
            Console.WriteLine("The disposed context refuses further use (ObjectDisposedException)");
        }
    }

    // 3. The Fluent API refines the conventions: lengths, precision, unique indexes, delete behaviour.
    public void ExplainFluentApiConfiguration()
    {
        PrintHeading("3. Fluent API - OnModelCreating refines the conventions");

        // BankContext (BankContexts.cs) has the same two DbSets as method 2's context plus an OnModelCreating.
        // Ask both for their script, then print only the lines that differ ("-" = before, "+" = after).
        string before;
        using (PlainBankContext plain = new PlainBankContext())
        {
            before = plain.Database.GenerateCreateScript();
        }

        string after;
        using (BankContext configured = new BankContext())
        {
            after = configured.Database.GenerateCreateScript();
        }

        PrintScriptChanges(before, after);

        // What each configuration line did:
        //  - HasMaxLength turned nvarchar(max) into nvarchar(100), (200) and (20), matching the SQL topics' tables.
        //  - HasIndex(...).IsUnique() added the two UNIQUE indexes: no two customers share an Email, no two accounts
        //    share an AccountNumber. The database enforces that, not the C# code.
        //  - OnDelete(DeleteBehavior.Restrict) changed ON DELETE CASCADE to NO ACTION: SQL Server now refuses to
        //    delete a customer who still has accounts - safer for a bank than silently deleting them.
        //  - IsRequired() changed nothing visible: FullName and Email are non-nullable `string`s, so they were already
        //    NOT NULL. It is still worth writing, because it keeps the rule when someone later changes a type to `string?`.
        //  - HasPrecision(18, 2) changed nothing visible either: with no setting, EF Core falls back to
        //    decimal(18,2) in this version and warns that values which do not fit would be silently truncated. Writing it down
        //    makes the size of the money column a decision you made and silences that warning.
    }

    // 4. A migration is a C# file that records one change to the database structure.
    public void ExplainMigrations()
    {
        PrintHeading("4. Migrations - the database structure, versioned in C# files");

        // A migration is a C# class made by the dotnet-ef command-line tool. This one was made, from the repository root, with:
        //   dotnet ef migrations add InitialCreate --project BasicToAdvancedLearning.Console --context BankContext --output-dir EfCoreBasics/Migrations
        // It lives in the Migrations folder next to this file and has two parts:
        //   Up()   - the change: CreateTable("Customers"), CreateTable("Accounts"), the indexes...
        //   Down() - the undo: here DropTable("Accounts") then DropTable("Customers").
        // Next to it is BankContextModelSnapshot.cs, the model as of the last migration. The next
        // `migrations add` compares your classes with that snapshot and writes only what changed.
        // The file name starts with the time it was made, so migrations sort in the order they were created.
        ResetTables();

        using (BankContext context = new BankContext())
        {
            Console.WriteLine("Migrations found in the code: " + string.Join(", ", context.Database.GetMigrations()));
            Console.WriteLine("Applied to the database yet: " + CountText(context.Database.GetAppliedMigrations()));
            Console.WriteLine("Pending (in code, not applied): " + string.Join(", ", context.Database.GetPendingMigrations()));

            // Migrate() creates the history table (__EFMigrationsHistory) if needed, runs the Up() of every pending
            // migration and records each one in that table. `dotnet ef database update` does the same from the command line.
            context.Database.Migrate();

            Console.WriteLine("Applied after Migrate(): " + string.Join(", ", context.Database.GetAppliedMigrations()));
            Console.WriteLine("Pending after Migrate(): " + CountText(context.Database.GetPendingMigrations()));

            // Calling it again is safe: nothing is pending, so nothing runs.
            context.Database.Migrate();
        }

        Console.WriteLine("Tables in the database now:");
        SqlLab.Show("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME IN ('Customers', 'Accounts', '__EFMigrationsHistory') ORDER BY TABLE_NAME");
        Console.WriteLine("EF Core's own record of what it has applied:");
        SqlLab.Show("SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId");
    }

    // 5. EnsureCreated builds tables straight from the model and records nothing; Migrate follows the migration files.
    public void ExplainEnsureCreatedVsMigrate()
    {
        PrintHeading("5. EnsureCreated vs Migrate - pick one per database");

        // EnsureCreated() builds the tables straight from the model in one step. It knows nothing about migrations
        // and writes no __EFMigrationsHistory table, so it suits a throwaway database (a quick experiment or a test).
        // Migrate() instead runs the migration files and records them. They cannot take turns on one database:
        //  - Order A: EnsureCreated first. The tables exist but no migration is recorded, so Migrate() believes
        //    InitialCreate is still pending, tries to CREATE the tables again and SQL Server refuses (error 2714).
        //    (Migrate() has by then created its own history table, but InitialCreate is not recorded in it.)
        //  - Order B: Migrate first. EnsureCreated() sees a database that already has tables and does nothing.
        // So choose per database: migrations for a database that will change over time, EnsureCreated for a throwaway one.

        // Order A: EnsureCreated first, then Migrate.
        ResetTables();
        using (BankContext context = new BankContext())
        {
            bool created = context.Database.EnsureCreated();
            Console.WriteLine($"Order A: EnsureCreated() created the tables: {created}");
        }
        Console.WriteLine("Tables after EnsureCreated():");
        ShowTableNames();

        using (BankContext context = new BankContext())
        {
            try
            {
                context.Database.Migrate();
                Console.WriteLine("Migrate() worked");
            }
            catch (SqlException ex)
            {
                // Error 2714 is SQL Server's "there is already an object with that name".
                Console.WriteLine($"Migrate() failed (SQL Server error {ex.Number}): the tables were already there");
            }
        }
        // Migrate() did create its history table before it failed, but no migration was recorded in it.
        Console.WriteLine("Tables after the failed Migrate():");
        ShowTableNames();
        SqlLab.Show("SELECT COUNT(*) AS RowsInHistory FROM __EFMigrationsHistory");

        // Order B: Migrate first, then EnsureCreated.
        ResetTables();
        using (BankContext context = new BankContext())
        {
            context.Database.Migrate();
            bool created = context.Database.EnsureCreated();
            Console.WriteLine($"Order B: after Migrate(), EnsureCreated() created anything: {created}");
        }
    }

    // Drops this lesson's tables, children before parents (a foreign key blocks dropping a parent first), so every
    // run starts from the same empty state. Transactions is included in case a table from the bank example is left over.
    private static void ResetTables()
    {
        SqlLab.Run("DROP TABLE IF EXISTS Transactions; DROP TABLE IF EXISTS Accounts; DROP TABLE IF EXISTS Customers; DROP TABLE IF EXISTS __EFMigrationsHistory;");
    }

    private static void ShowTableNames()
    {
        SqlLab.Show("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME IN ('Customers', 'Accounts', '__EFMigrationsHistory') ORDER BY TABLE_NAME");
    }

    private static string CountText(IEnumerable<string> names)
    {
        List<string> list = names.ToList();
        return list.Count == 0 ? "(none)" : string.Join(", ", list);
    }

    // Prints the script lines that exist only in the first script (-) or only in the second (+).
    // Except() keeps the lines that appear in only one of the two scripts.
    private static void PrintScriptChanges(string before, string after)
    {
        string[] beforeLines = before.Split('\n').Select(l => l.TrimEnd()).Where(l => l.Length > 0).ToArray();
        string[] afterLines = after.Split('\n').Select(l => l.TrimEnd()).Where(l => l.Length > 0).ToArray();
        foreach (string line in beforeLines.Except(afterLines)) Console.WriteLine("- " + line);
        foreach (string line in afterLines.Except(beforeLines)) Console.WriteLine("+ " + line);
    }

    private static void PrintHeading(string title)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', title.Length));
        Console.WriteLine(title);
        Console.WriteLine(new string('=', title.Length));
    }
}
