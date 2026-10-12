using BasicToAdvancedLearning.Interfaces;

namespace BasicToAdvancedLearning.SqlBasics;

// Topic 11: SQL basics — SELECT, WHERE, ORDER BY, JOIN and GROUP BY, written in T-SQL (SQL Server's dialect of SQL).
// The running example is the bank: Customers, Accounts and Transactions (BankSeed.cs creates and fills them;
// Topic 10 explains their design). A query only READS data — it never changes a table (Topic 12 does that).
// Each query below is a C# string. SqlLab.Show sends it to SQL Server and prints the rows that come back;
// how C# really talks to SQL Server (SqlConnection, SqlCommand) is Topic 13, so it is hidden for now.
// A """ ... """ string is a raw string literal: C# keeps the text exactly as written, line breaks included,
// which lets each SQL clause sit on its own line.
// See ArchitectureDiagrams/SqlBasics for the visual walkthrough.
public class SqlBasicsDemo
{
    // SELECT chooses COLUMNS, WHERE chooses ROWS.
    public void ExplainSelectAndWhere()
    {
        PrintHeading("1. SELECT and WHERE — choose the columns, then choose the rows");

        // * means "every column". Handy for a first look. Real code usually names the columns it needs, so the
        // result stays the same shape even if someone later adds a column to the table.
        // (ORDER BY AccountId only makes the print order predictable; method 3 explains why that matters.)
        string allColumns = """
            SELECT *
            FROM Accounts
            ORDER BY AccountId
            """;
        RunQuery(allColumns);

        // Naming the columns: only those come back, in the order you wrote them.
        string twoColumns = """
            SELECT AccountNumber, Balance
            FROM Accounts
            ORDER BY AccountId
            """;
        RunQuery(twoColumns);

        // WHERE keeps only the rows for which the condition is true. Text goes in single quotes, and equality is
        // a single = (not ==). The other comparisons are <>  (not equal; T-SQL also accepts !=), >, <, >= and <=.
        string savings = """
            SELECT AccountNumber, Balance
            FROM Accounts
            WHERE AccountType = 'Savings'
            ORDER BY AccountId
            """;
        RunQuery(savings);

        // AND: both conditions must be true. (If you mix AND with OR, AND is worked out first —
        // add parentheses so the reader does not have to remember that.)
        string bigCurrent = """
            SELECT AccountNumber, AccountType, Balance
            FROM Accounts
            WHERE AccountType <> 'Savings' AND Balance > 100
            ORDER BY AccountId
            """;
        RunQuery(bigCurrent);

        // OR: at least one condition must be true.
        string emptyOrHuge = """
            SELECT AccountNumber, Balance
            FROM Accounts
            WHERE Balance = 0 OR Balance > 3000
            ORDER BY AccountId
            """;
        RunQuery(emptyOrHuge);
    }

    // Shortcuts for common filters — and the one value that breaks the usual rules: NULL.
    public void ExplainHandyFilters()
    {
        PrintHeading("2. Handy filters — IN, BETWEEN, LIKE, and NULL");

        // IN (...) is a shorter way to write several = tests joined by OR.
        string inList = """
            SELECT AccountNumber, Balance
            FROM Accounts
            WHERE AccountNumber IN ('ACC-1001', 'ACC-3001')
            ORDER BY AccountId
            """;
        RunQuery(inList);

        // BETWEEN includes BOTH ends: 250 and 1500 are in the result because two balances are exactly those numbers.
        string balanceRange = """
            SELECT AccountNumber, Balance
            FROM Accounts
            WHERE Balance BETWEEN 250 AND 1500
            ORDER BY AccountId
            """;
        RunQuery(balanceRange);

        // LIKE matches a pattern: % stands for any run of characters (even none).
        // Whether LIKE ignores upper/lower case depends on the database's collation (its text-comparison rules),
        // so write patterns that do not depend on it.
        string startsWithC = """
            SELECT CustomerId, FullName
            FROM Customers
            WHERE FullName LIKE 'C%'
            ORDER BY CustomerId
            """;
        RunQuery(startsWithC);

        // _ stands for exactly ONE character: 'B_n%' means B, any one letter, n, then anything.
        string oneLetterWildcard = """
            SELECT CustomerId, FullName
            FROM Customers
            WHERE FullName LIKE 'B_n%'
            ORDER BY CustomerId
            """;
        RunQuery(oneLetterWildcard);

        // NULL means "no value / unknown". Every column in our bank tables is NOT NULL, so the first NULL
        // you will see comes from a join (method 4). To show the rule we use NULL directly.
        // NULL = NULL is not true — it is "unknown" — and WHERE keeps only rows whose condition is TRUE,
        // so this query returns no rows. (NULL <> 5 is unknown too, not true.)
        string nullEquals = """
            SELECT 'matched' AS Result
            WHERE NULL = NULL
            """;
        RunQuery(nullEquals);

        // IS NULL is the test that works.
        string nullIs = """
            SELECT 'matched' AS Result
            WHERE NULL IS NULL
            """;
        RunQuery(nullIs);
    }

    // Without ORDER BY the order of the rows is not promised. TOP keeps only the first few rows.
    public void ExplainOrderingAndTop()
    {
        PrintHeading("3. ORDER BY and TOP — decide the order, take the first few");

        // No ORDER BY: SQL Server may return the rows in any order it likes. On a tiny table it usually looks
        // tidy, but nothing promises that — so a query whose result order matters must say ORDER BY.
        string unordered = """
            SELECT AccountNumber, Balance
            FROM Accounts
            """;
        RunQuery(unordered);

        // ORDER BY Balance DESC: biggest first. ASC (smallest first) is the default when you write nothing.
        // Two accounts both hold 0, so a second sort column (AccountNumber) settles their order.
        string richestFirst = """
            SELECT AccountNumber, Balance
            FROM Accounts
            ORDER BY Balance DESC, AccountNumber
            """;
        RunQuery(richestFirst);

        // TOP 3 keeps the first three rows of the result. Each Transactions row is one deposit or withdrawal;
        // In our seed data Amount is always positive; Kind says which way the money moved.
        // TOP only means "the biggest" or "the newest" if ORDER BY puts them first — without ORDER BY you get
        // some three rows. (TOP is T-SQL; other database systems spell it differently.)
        string topThree = """
            SELECT TOP 3 TransactionId, AccountId, Amount, Kind
            FROM Transactions
            ORDER BY Amount DESC, TransactionId
            """;
        RunQuery(topThree);
    }

    // A join glues rows from two tables together using a matching column.
    public void ExplainJoins()
    {
        PrintHeading("4. JOIN — Customers and Accounts side by side");

        // Accounts.CustomerId holds the CustomerId of the account's owner. ON says which columns must match.
        // c and a are aliases: short names for the tables, so we can write c.FullName instead of Customers.FullName.
        // INNER JOIN keeps only pairs that match, so Dana Smith (no account) disappears from the result.
        string innerJoin = """
            SELECT c.FullName, a.AccountNumber, a.AccountType
            FROM Customers c
            INNER JOIN Accounts a ON a.CustomerId = c.CustomerId
            ORDER BY c.CustomerId, a.AccountId
            """;
        RunQuery(innerJoin);

        // LEFT JOIN keeps EVERY row of the left table (Customers). Where there is no match, the right-hand
        // columns are filled with NULL — Dana now appears, with NULL for the account columns.
        string leftJoin = """
            SELECT c.FullName, a.AccountNumber, a.AccountType
            FROM Customers c
            LEFT JOIN Accounts a ON a.CustomerId = c.CustomerId
            ORDER BY c.CustomerId, a.AccountId
            """;
        RunQuery(leftJoin);

        // Those NULLs are useful: LEFT JOIN plus IS NULL finds the rows that have NO match.
        // (Test a column that can never be NULL in a real row, such as the account's primary key.)
        string noAccount = """
            SELECT c.FullName
            FROM Customers c
            LEFT JOIN Accounts a ON a.CustomerId = c.CustomerId
            WHERE a.AccountId IS NULL
            """;
        RunQuery(noAccount);
    }

    // Aggregate functions boil many rows down to one value; GROUP BY does that once per group.
    public void ExplainGroupByAndAggregates()
    {
        PrintHeading("5. Aggregates and GROUP BY — many rows in, one row per group out");

        // COUNT, SUM, AVG, MIN and MAX each turn many rows into ONE value. With no GROUP BY, the whole table is one group.
        string wholeTable = """
            SELECT COUNT(*) AS Accounts, SUM(Balance) AS TotalBalance, AVG(Balance) AS AverageBalance,
                   MIN(Balance) AS Lowest, MAX(Balance) AS Highest
            FROM Accounts
            """;
        RunQuery(wholeTable);

        // GROUP BY AccountType makes one group per type and the aggregates are worked out inside each group.
        // Every column you select from the table must be either in the GROUP BY or inside an aggregate.
        string perType = """
            SELECT AccountType, COUNT(*) AS Accounts, SUM(Balance) AS Total
            FROM Accounts
            GROUP BY AccountType
            ORDER BY AccountType
            """;
        RunQuery(perType);

        // Group a LEFT JOIN to count each customer's accounts — including Dana, who has none.
        // COUNT(*) counts rows, and Dana's single joined row counts as 1. COUNT(a.AccountId) counts only
        // non-NULL values, so Dana gets 0. SUM adds up non-NULL values; with nothing to add it gives NULL, not 0.
        string perCustomer = """
            SELECT c.FullName, COUNT(*) AS JoinedRows, COUNT(a.AccountId) AS Accounts, SUM(a.Balance) AS Total
            FROM Customers c
            LEFT JOIN Accounts a ON a.CustomerId = c.CustomerId
            GROUP BY c.CustomerId, c.FullName
            ORDER BY c.CustomerId
            """;
        RunQuery(perCustomer);
    }

    // WHERE filters rows BEFORE grouping; HAVING filters groups AFTER. The clauses are written in one order
    // but worked out (logically) in another — and that explains most beginner errors.
    public void ExplainWhereVsHaving()
    {
        PrintHeading("6. WHERE vs HAVING — and the order SQL really thinks in");

        // Read it in the order SQL works it out:
        //   FROM Accounts          -> start with all 6 rows
        //   WHERE Balance > 0      -> drop the two empty accounts: 4 rows left
        //   GROUP BY AccountType   -> 2 groups (Current, Savings), each now holding 2 rows
        //   HAVING SUM(Balance) > 2000 -> keep only groups whose total is over 2000: Savings
        //   SELECT ...             -> compute the output columns (this is when the alias Total comes into existence)
        //   ORDER BY Total DESC    -> sort what is left; ORDER BY comes after SELECT, so it CAN use the alias
        // Compare with method 5: there each type counted 3 accounts; here WHERE has already removed the empty ones.
        string filteredGroups = """
            SELECT AccountType, COUNT(*) AS Accounts, SUM(Balance) AS Total
            FROM Accounts
            WHERE Balance > 0
            GROUP BY AccountType
            HAVING SUM(Balance) > 2000
            ORDER BY Total DESC
            """;
        RunQuery(filteredGroups);

        Console.WriteLine();
        Console.WriteLine("Logical order: FROM (with its JOINs) -> WHERE -> GROUP BY -> HAVING -> SELECT -> ORDER BY (then TOP).");
        Console.WriteLine("It is the order to REASON in; SQL Server may run the steps differently inside, but the answer is the same.");

        // WHERE runs before GROUP BY, so no group totals exist yet — an aggregate used directly in WHERE is rejected (error 147; use HAVING).
        string aggregateInWhere = """
            SELECT AccountType
            FROM Accounts
            WHERE SUM(Balance) > 2000
            GROUP BY AccountType
            """;
        RunRejectedQuery(aggregateInWhere);

        // WHERE also runs before SELECT, so the alias Total does not exist yet — rejected (error 207, "invalid column name").
        string aliasInWhere = """
            SELECT AccountType, SUM(Balance) AS Total
            FROM Accounts
            WHERE Total > 2000
            GROUP BY AccountType
            """;
        RunRejectedQuery(aliasInWhere);
    }

    // Shows the SQL text, then lets SqlLab.Show run it on SQL Server and print the rows that come back.
    private static void RunQuery(string sql)
    {
        PrintSql(sql);
        Console.WriteLine("  Result:");
        SqlLab.Show(sql);
    }

    // For queries SQL Server is EXPECTED to refuse: SqlLab.ShowError prints the error number it reports.
    private static void RunRejectedQuery(string sql)
    {
        PrintSql(sql);
        Console.WriteLine("  Result:");
        SqlLab.ShowError(sql);
    }

    private static void PrintSql(string sql)
    {
        Console.WriteLine();
        Console.WriteLine("  SQL:");
        foreach (string line in sql.Split('\n'))
        {
            Console.WriteLine("    " + line.TrimEnd('\r'));
        }
    }

    private static void PrintHeading(string title)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', title.Length));
        Console.WriteLine(title);
        Console.WriteLine(new string('=', title.Length));
    }
}
