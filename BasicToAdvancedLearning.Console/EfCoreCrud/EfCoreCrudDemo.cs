using Microsoft.EntityFrameworkCore;

namespace BasicToAdvancedLearning.EfCoreCrud;

// Topic 15: Entity Framework Core CRUD — Create, Read, Update, Delete on the bank database WITHOUT writing SQL by
// hand. EF Core is an "ORM": it turns C# objects into table rows and LINQ (Topic 9) into SQL. The running example is
// the bank (Customers, Accounts, Transactions). Every method prints the REAL SQL that EF sent, through the SqlSpy
// helper, so nothing is magic. The tables are created by EnsureCreated (migrations — versioned schema changes —
// were covered in Topic 14; here EnsureCreated keeps the lesson short). Each method uses its own short-lived BankContext. Needs SQL Server (see SqlLab).
// See ArchitectureDiagrams/EfCoreCrud for the visual walkthrough.
public class EfCoreCrudDemo
{
    private const string AshaEmail = "asha@example.com";
    private const string BenEmail = "ben@example.com";
    private const string BenName = "Ben Carter";
    private const string BenAccountNumber = "ACC-2001";
    private const string AshaAccountNumber = "ACC-1001";

    // Note: each method creates a fresh SqlSpy and BankContext to keep the lesson simple. That is fine here, but not
    // the pattern for a real app: EF builds its internal services once per distinct set of options, and a new
    // interceptor instance each time means a new set (slow and memory-hungry if repeated). Real apps register one.

    // CREATE: Add puts an object in the change tracker; SaveChanges sends the INSERTs.
    public void ExplainAddAndSave()
    {
        PrintHeading("1. Add and SaveChanges — create rows from objects");

        var spy = new SqlSpy { Print = false };   // quiet while the tables are built
        using var context = new BankContext(spy);

        // Start from nothing so the lesson can be run again and again. EnsureDeleted removes the WHOLE database, not
        // just some tables, so only ever point it at a scratch database: SqlLab gives this lesson its own.
        // EnsureCreated then builds it again with tables made from the entity classes (see BankContext).
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        Console.WriteLine("Database dropped and recreated (EnsureDeleted removes the WHOLE database, so use a scratch one): Customers, Accounts and Transactions are empty.");
        Console.WriteLine("  (here: this lesson's own scratch database)");
        spy.Reset();
        spy.Print = true;

        var asha = new Customer { FullName = "Asha Rao", Email = AshaEmail };
        Console.WriteLine($"New object: CustomerId = {asha.CustomerId}, CreatedAt set by us? {asha.CreatedAt != default}");

        // Add only tells EF "track this object as new". Nothing has gone to SQL Server yet.
        context.Customers.Add(asha);
        Console.WriteLine($"After Add:  state = {context.Entry(asha).State}, SQL commands sent so far = {spy.Count}");

        // SaveChanges sends everything pending and returns how many rows it wrote. This is the INSERT EF writes:
        // the OUTPUT part brings the database-made values (the identity number and the CreatedAt default) back.
        // The SET lines and OUTPUT 1 you will see in later SQL are housekeeping EF adds (no row-count noise, report
        // rows affected). BEGIN/COMMIT never appear in this text: EF starts the transaction through ADO.NET, not
        // as a SQL line.
        int rowsWritten = context.SaveChanges();
        spy.Print = false;   // later saves in THIS method use batched statements that are harder to read; we show counts instead
        Console.WriteLine($"SaveChanges wrote {rowsWritten} row(s).");

        // SQL Server made the identity number and the CreatedAt default; EF read them back into our object.
        Console.WriteLine($"After save: CustomerId = {asha.CustomerId}, CreatedAt filled in by the database? {asha.CreatedAt != default}, state = {context.Entry(asha).State}");

        // Adding one object adds the objects hanging off it too: EF works out the order and the foreign keys.
        var ben = new Customer { FullName = BenName, Email = BenEmail };
        ben.Accounts.Add(new Account { AccountNumber = BenAccountNumber, AccountType = AccountTypes.Current, Balance = 250m });
        ben.Accounts.Add(new Account { AccountNumber = "ACC-2002", AccountType = AccountTypes.Savings, Balance = 1000m });
        var chen = new Customer { FullName = "Chen Li", Email = "chen@example.com" };
        var ashaAccount = new Account { AccountNumber = AshaAccountNumber, AccountType = AccountTypes.Savings, Balance = 500m, CustomerId = asha.CustomerId };

        spy.Reset();
        context.Customers.AddRange(ben, chen);
        context.Accounts.Add(ashaAccount);
        rowsWritten = context.SaveChanges();
        Console.WriteLine($"Second SaveChanges wrote {rowsWritten} rows (2 customers + 3 accounts) in {spy.Count} SQL command(s): EF groups statements.");

        Console.WriteLine($"Ben's accounts now have ids {ben.Accounts[0].AccountId} and {ben.Accounts[1].AccountId}, both with CustomerId {ben.CustomerId} (the foreign key was filled in).");

        // Two deposits on Asha's account, so the later lessons have child rows to work with.
        ashaAccount.Transactions.Add(new BankTransaction { Amount = 300m, Kind = TransactionKinds.Deposit });
        ashaAccount.Transactions.Add(new BankTransaction { Amount = 200m, Kind = TransactionKinds.Deposit });
        rowsWritten = context.SaveChanges();
        Console.WriteLine($"Third SaveChanges wrote {rowsWritten} rows (2 deposits).");
    }

    // READ: a LINQ query over a DbSet becomes SQL — but only when something enumerates it.
    public void ExplainQueryingWithLinq()
    {
        PrintHeading("2. Querying with LINQ — a query is SQL waiting to run");

        var spy = new SqlSpy();
        using var context = new BankContext(spy);

        // Where/OrderBy build a query description. This is Topic 9's deferred execution again.
        IQueryable<Customer> query = context.Customers
            .Where(c => c.FullName.StartsWith("B") || c.FullName.StartsWith("C"))
            .OrderBy(c => c.FullName);
        Console.WriteLine($"Query built. SQL commands sent so far: {spy.Count}");

        // ToQueryString shows the SQL EF WOULD send, without sending it.
        Console.WriteLine("ToQueryString() gives:");
        Console.WriteLine(Indent(query.ToQueryString()));
        Console.WriteLine($"Still no SQL sent: {spy.Count}");

        // ToList enumerates the query: NOW the SQL goes out, and the rows come back as Customer objects.
        List<Customer> customers = query.ToList();
        Console.WriteLine($"After ToList: {spy.Count} command(s) sent, {customers.Count} customers: {string.Join(", ", customers.Select(c => c.FullName))}");

        // Select asks for only some columns. EF's SQL then names only those columns.
        spy.Reset();
        List<string> names = context.Customers.OrderBy(c => c.FullName).Select(c => c.FullName).ToList();
        Console.WriteLine($"Names only: {string.Join(", ", names)}");

        spy.Reset();
        // FirstOrDefault runs the query and returns the first match, or null when there is none.
        Customer? missing = context.Customers.FirstOrDefault(c => c.Email == "nobody@example.com");
        Console.WriteLine($"FirstOrDefault for an unknown email: {(missing == null ? "null" : missing.FullName)}");

        // Find looks up by primary key. If the object is already tracked it is returned without asking the database.
        spy.Reset();
        Customer? first = context.Customers.Find(1);
        Console.WriteLine($"Find(1) -> {first?.FullName}; SQL commands used: {spy.Count}");
        spy.Reset();
        Customer? again = context.Customers.Find(1);
        Console.WriteLine($"Find(1) again -> {again?.FullName}; SQL commands used: {spy.Count}; same object? {ReferenceEquals(first, again)}");

        ShowQueryableVersusEnumerable(context, spy);
    }

    // The difference between filtering IN SQL Server and filtering in C# after pulling everything.
    private static void ShowQueryableVersusEnumerable(BankContext context, SqlSpy spy)
    {
        Console.WriteLine();
        Console.WriteLine("IQueryable vs IEnumerable — where does the Where run?");

        // DbSet is an IQueryable: Where is turned into SQL, so SQL Server does the filtering.
        spy.Reset();
        List<Customer> filteredInSql = context.Customers.AsNoTracking().Where(c => c.FullName.StartsWith("B")).ToList();
        Console.WriteLine($"  Where on the IQueryable: {filteredInSql.Count} row(s) came back.");

        // AsEnumerable switches to plain LINQ-to-objects: the SQL just reads the whole table, C# filters.
        spy.Reset();
        List<Customer> filteredInCSharp = context.Customers.AsNoTracking().AsEnumerable().Where(c => c.FullName.StartsWith("B")).ToList();
        Console.WriteLine($"  Where after AsEnumerable: {filteredInCSharp.Count} row(s) kept, but every Customer row was read first.");
    }

    // Related data: a navigation property is EMPTY unless you ask for it. Include (a JOIN), N+1, and Select.
    public void ExplainRelatedData()
    {
        PrintHeading("3. Related data — Include, the N+1 trap, and Select");

        var spy = new SqlSpy { Print = false };
        using var context = new BankContext(spy);

        // AsNoTracking means "just give me the objects, do not register them in the change tracker" (method 4 shows
        // what tracking is for). The entity queries here use AsNoTracking; the Select returns plain values, so nothing is tracked either.
        // No Include: customers come back, but their Accounts lists are NOT loaded. EF Core does not fetch them behind
        // your back (that would need "lazy loading", which is not switched on here).
        List<Customer> plain = context.Customers.AsNoTracking().OrderBy(c => c.FullName).ToList();
        Console.WriteLine($"Without Include ({spy.Count} command):");
        foreach (Customer c in plain)
        {
            Console.WriteLine($"  {c.FullName}: Accounts.Count = {c.Accounts.Count}");
        }
        Console.WriteLine("  (Ben really has 2 accounts: the list is empty only because it was never loaded.)");

        // Include asks EF to JOIN the accounts in the same query.
        spy.Reset();
        spy.Print = true;
        Console.WriteLine("With Include, EF sends this SQL:");
        List<Customer> withAccounts = context.Customers.AsNoTracking().Include(c => c.Accounts).OrderBy(c => c.FullName).ToList();
        spy.Print = false;
        Console.WriteLine($"With Include ({spy.Count} command, a LEFT JOIN so customers without accounts still appear):");
        foreach (Customer c in withAccounts)
        {
            Console.WriteLine($"  {c.FullName}: Accounts.Count = {c.Accounts.Count}");
        }

        // The N+1 trap: one query for the customers, then one more query PER customer for its accounts.
        spy.Reset();
        int accountsFound = 0;
        foreach (Customer c in context.Customers.AsNoTracking().OrderBy(c => c.FullName).ToList())
        {
            accountsFound += context.Accounts.AsNoTracking().Where(a => a.CustomerId == c.CustomerId).ToList().Count;
        }
        Console.WriteLine($"N+1 loop: found {accountsFound} accounts using {spy.Count} SQL commands (1 + one per customer).");

        // Select shapes the result: only the columns you name are fetched, and no Include is needed.
        spy.Reset();
        spy.Print = true;
        Console.WriteLine("Select with counts and sums, EF sends this SQL:");
        var summaries = context.Customers.OrderBy(c => c.FullName)
            .Select(c => new { c.FullName, AccountCount = c.Accounts.Count, TotalBalance = c.Accounts.Sum(a => a.Balance) })
            .ToList();
        spy.Print = false;
        foreach (var s in summaries)
        {
            Console.WriteLine($"  {s.FullName}: {s.AccountCount} account(s), total balance {s.TotalBalance}");
        }
        Console.WriteLine($"Select used {spy.Count} command.");
    }

    // UPDATE: change a tracked object, then SaveChanges — EF compares with its snapshot and updates what changed.
    public void ExplainUpdatingTrackedEntities()
    {
        PrintHeading("4. Updating — the change tracker notices edits");

        var spy = new SqlSpy { Print = false };
        using (var context = new BankContext(spy))
        {
            // A normal query TRACKS what it returns: EF keeps each object plus a snapshot of its original values.
            // (Single asks SQL for TOP(2) so EF can tell "exactly one match" from "more than one".)
            Account account = context.Accounts.Single(a => a.AccountNumber == BenAccountNumber);
            Console.WriteLine($"Loaded {account.AccountNumber}: balance {account.Balance}, state = {context.Entry(account).State}");

            account.Balance += 100m;   // just a C# property change; no SQL yet
            var entry = context.Entry(account);
            Console.WriteLine($"After edit: state = {entry.State}, Balance modified? {entry.Property(a => a.Balance).IsModified}, AccountNumber modified? {entry.Property(a => a.AccountNumber).IsModified}");
            Console.WriteLine($"  original Balance = {entry.Property(a => a.Balance).OriginalValue}, current Balance = {entry.Property(a => a.Balance).CurrentValue}");

            spy.Reset();
            int rows = SaveShowingSql(context, spy);
            Console.WriteLine($"SaveChanges wrote {rows} row; the UPDATE set only the changed column. State is now {context.Entry(account).State}.");

            spy.Reset();
            Console.WriteLine($"SaveChanges again with nothing changed wrote {context.SaveChanges()} rows, SQL commands: {spy.Count}.");
        }

        // AsNoTracking: handy for read-only work, but EF keeps no snapshot, so it cannot notice edits.
        Account detached;
        using (var context = new BankContext(spy))
        {
            detached = context.Accounts.AsNoTracking().Single(a => a.AccountNumber == BenAccountNumber);
            detached.Balance += 50m;
            Console.WriteLine($"AsNoTracking object edited to {detached.Balance}: state = {context.Entry(detached).State}");
            spy.Reset();
            int rows = context.SaveChanges();
            Console.WriteLine($"SaveChanges wrote {rows} rows using {spy.Count} SQL commands — the edit was not saved.");
        }

        using (var context = new BankContext(spy))
        {
            decimal stored = context.Accounts.AsNoTracking().Single(a => a.AccountNumber == BenAccountNumber).Balance;
            Console.WriteLine($"Balance in the database: {stored}");

            // Update() starts tracking an object we already hold and marks every non-key column as changed, because EF
            // has no snapshot to compare with: it cannot know which column really differs.
            spy.Reset();
            context.Update(detached);
            Console.WriteLine($"After Update(detached): state = {context.Entry(detached).State}");
            Console.WriteLine($"SaveChanges wrote {SaveShowingSql(context, spy)} row.");
        }
    }

    // DELETE: Remove marks a row for deletion; what happens to related rows depends on the foreign key.
    public void ExplainRemovingRows()
    {
        PrintHeading("5. Removing rows — and what the foreign keys say");

        var spy = new SqlSpy { Print = false };

        // A customer with no accounts: nothing refers to the row, so the DELETE succeeds.
        using (var context = new BankContext(spy))
        {
            Customer chen = context.Customers.Single(c => c.FullName == "Chen Li");
            context.Customers.Remove(chen);
            Console.WriteLine($"After Remove: state = {context.Entry(chen).State}");
            Console.WriteLine($"SaveChanges wrote {SaveShowingSql(context, spy)} row.");
        }

        // A customer who still has accounts: the foreign key makes SQL Server refuse. (BankContext set this key to
        // "no action on delete" on purpose, so the database protects the accounts.)
        using (var context = new BankContext(spy))
        {
            Customer ben = context.Customers.Single(c => c.FullName == BenName);
            context.Customers.Remove(ben);
            try
            {
                SaveShowingSql(context, spy);
            }
            catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sql)
            {
                // 547 is SQL Server's number for a FOREIGN KEY / CHECK conflict. EF wraps the SqlException.
                Console.WriteLine($"Removing Ben failed: {ex.GetType().Name}, SQL Server error number {sql.Number}.");
            }
        }

        // Remove the children first (same SaveChanges): EF orders the DELETEs so the accounts go before the customer.
        using (var context = new BankContext(spy))
        {
            Customer ben = context.Customers.Include(c => c.Accounts).Single(c => c.FullName == BenName);
            context.Accounts.RemoveRange(ben.Accounts);
            context.Customers.Remove(ben);
            Console.WriteLine($"Accounts removed together with the customer: SaveChanges wrote {SaveShowingSql(context, spy)} rows.");
        }

        // Cascade: Transactions.AccountId was created with ON DELETE CASCADE, so SQL Server deletes the children.
        using (var context = new BankContext(spy))
        {
            Account account = context.Accounts.Single(a => a.AccountNumber == AshaAccountNumber);
            int before = context.Transactions.Count(t => t.AccountId == account.AccountId);
            context.Accounts.Remove(account);
            int rows = SaveShowingSql(context, spy);
            int after = context.Transactions.Count();
            // SaveChanges counts the row it asked to delete (1). SQL Server removed the transactions by itself.
            Console.WriteLine($"Account ACC-1001 had {before} transactions. SaveChanges wrote {rows} row; transactions left in the table: {after}.");
        }
    }

    // SaveChanges is one unit: if any command fails, none of the changes are kept.
    public void ExplainSaveChangesIsAllOrNothing()
    {
        PrintHeading("6. SaveChanges is all or nothing");

        var spy = new SqlSpy { Print = false };
        using var context = new BankContext(spy);

        // Two pending changes: a valid rename of Asha, and a new customer that re-uses Asha's email (UNIQUE).
        Customer asha = context.Customers.Single(c => c.Email == AshaEmail);
        asha.FullName = "Asha R. Rao";
        context.Customers.Add(new Customer { FullName = "Dev Again", Email = AshaEmail });

        try
        {
            SaveShowingSql(context, spy);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sql)
        {
            // 2601: a unique index refused a duplicate value.
            Console.WriteLine($"SaveChanges failed: SQL Server error number {sql.Number}.");
        }

        // The rename was sent first in the same batch and was valid, but SaveChanges wraps multi-statement saves in a database
        // transaction (EF starts it for you), so the failure undid the rename too.
        using var fresh = new BankContext();
        Console.WriteLine($"Name stored for {AshaEmail}: {fresh.Customers.Single(c => c.Email == AshaEmail).FullName}");
        Console.WriteLine($"Customers in the table: {fresh.Customers.Count()}");
    }

    // Runs SaveChanges with the spy switched on, so the lesson shows the SQL of the write only.
    private static int SaveShowingSql(BankContext context, SqlSpy spy)
    {
        spy.Reset();
        spy.Print = true;
        try
        {
            return context.SaveChanges();
        }
        finally
        {
            spy.Print = false;
        }
    }

    private static string Indent(string text) => "    " + text.Replace("\r\n", "\n").Replace("\n", "\n    ");

    private static void PrintHeading(string title)
    {
        Console.WriteLine();
        Console.WriteLine($"=== {title} ===");
    }
}
