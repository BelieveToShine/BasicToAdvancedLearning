using BasicToAdvancedLearning.Interfaces;

namespace BasicToAdvancedLearning.RelationalConcepts;

// Topic 10: Relational database concepts — tables, rows and columns, primary keys, foreign keys,
// constraints and NULL, and normalization. Everything is T-SQL sent to SQL Server through SqlLab
// (Interfaces/SqlLab.cs), so this lesson stays about the database itself. How C# talks to SQL Server
// in real code (SqlConnection, SqlCommand) is Topic 13.
//
// The running example is a small bank with three tables:
//   Customers (one row per person)  ->  Accounts (one row per account)  ->  Transactions (one row per deposit/withdrawal)
// Every method starts by dropping and re-creating the tables it needs, so you can run the lesson as
// many times as you like and get the same output. Each concept gets its own method so it can be
// explained one at a time from RelationalConceptsExample.
// See ArchitectureDiagrams/RelationalConcepts for the visual walkthrough.
public class RelationalConceptsDemo
{
    // 1. A table is a grid: columns say WHAT is stored, rows are the things stored.
    public void ExplainTablesRowsColumns()
    {
        PrintHeading("1. Tables, rows and columns — a grid with a fixed shape");
        DropAllTables();

        // CREATE TABLE fixes the columns and the data type of each one. A type is a promise about what
        // the column may hold: INT = whole number, NVARCHAR(100) = text up to 100 characters.
        string createCustomers = """
            CREATE TABLE Customers (
                CustomerId INT,
                FullName   NVARCHAR(100),
                Email      NVARCHAR(200)
            )
            """;
        Execute(createCustomers);

        // Each row is one customer: one value per column. A multi-row INSERT adds three at once.
        string insertCustomers = """
            INSERT INTO Customers (CustomerId, FullName, Email) VALUES
                (1, 'Asha Rao',    'asha@example.com'),
                (2, 'Ben Carter',  'ben@example.com'),
                (3, 'Chen Li',     'chen@example.com')
            """;
        Execute(insertCustomers);

        // SELECT reads rows back. A table has no built-in order, so ORDER BY makes the output predictable.
        Query("SELECT CustomerId, FullName, Email FROM Customers ORDER BY CustomerId");

        // SQL Server describes its own tables too: here are the columns we just declared.
        Console.WriteLine("The shape of the table (its columns and their types):");
        Query("""
            SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH AS MaxLength
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = 'Customers'
            ORDER BY ORDINAL_POSITION
            """);

        // The problem this table cannot solve yet: nothing says CustomerId must be different on each row.
        string duplicateAsha = "INSERT INTO Customers (CustomerId, FullName, Email) VALUES (1, 'Asha Rao', 'asha@example.com')";
        Console.WriteLine("Nothing stops us adding customer 1 a second time:");
        Execute(duplicateAsha);
        Query("SELECT CustomerId, FullName FROM Customers ORDER BY CustomerId, FullName");
        Console.WriteLine("Which row is 'the' customer 1? The table cannot tell. Next: primary keys.");
    }

    // 2. A primary key gives every row an identity that no other row can share.
    public void ExplainPrimaryKeys()
    {
        PrintHeading("2. Primary keys — every row gets a unique ID");
        DropAllTables();

        // PRIMARY KEY = this column's value must be unique and must not be NULL, so it can point at ONE row.
        // IDENTITY(1,1) = SQL Server hands out the numbers itself, starting at 1 and counting up by 1.
        // We name the constraint (PK_Customers) so error messages are easy to read.
        string createCustomers = """
            CREATE TABLE Customers (
                CustomerId INT IDENTITY(1,1) CONSTRAINT PK_Customers PRIMARY KEY,
                FullName   NVARCHAR(100),
                Email      NVARCHAR(200)
            )
            """;
        Execute(createCustomers);

        // We do not list CustomerId: the database fills it in.
        Execute("""
            INSERT INTO Customers (FullName, Email) VALUES
                ('Asha Rao',   'asha@example.com'),
                ('Ben Carter', 'ben@example.com'),
                ('Chen Li',    'chen@example.com')
            """);
        Query("SELECT CustomerId, FullName FROM Customers ORDER BY CustomerId");

        // Two different people can share a NAME, which is exactly why a name makes a poor key.
        // The ID is what tells them apart.
        Execute("INSERT INTO Customers (FullName, Email) VALUES ('Asha Rao', 'asha.rao@other.example')");
        Query("SELECT CustomerId, FullName, Email FROM Customers WHERE FullName = 'Asha Rao' ORDER BY CustomerId");

        // Normally IDENTITY refuses ids we type ourselves. IDENTITY_INSERT ON lets us force one, so we can
        // watch the PRIMARY KEY do its job: id 1 already exists, so SQL Server rejects the row (error 2627).
        string forceDuplicateId = """
            SET IDENTITY_INSERT Customers ON;
            INSERT INTO Customers (CustomerId, FullName, Email) VALUES (1, 'Impostor', 'impostor@example.com');
            """;
        Console.WriteLine("Now try to reuse an id that is taken:");
        Refused(forceDuplicateId);
        Console.WriteLine("The table still holds 4 customers; the bad row was never stored.");
    }

    // 3. A foreign key is a column that must point at a row that really exists in another table.
    public void ExplainForeignKeys()
    {
        PrintHeading("3. Foreign keys — a link that may only point at a row that exists");
        DropAllTables();

        Execute("""
            CREATE TABLE Customers (
                CustomerId INT IDENTITY(1,1) CONSTRAINT PK_Customers PRIMARY KEY,
                FullName   NVARCHAR(100),
                Email      NVARCHAR(200)
            )
            """);

        // Accounts.CustomerId stores the id of the owner. The FOREIGN KEY ... REFERENCES part tells SQL Server
        // to check, on every insert or update, that a Customers row with that id exists. (A foreign key
        // column that allows NULL may also be left empty; method 4 adds NOT NULL so an account must have an owner.)
        string createAccounts = """
            CREATE TABLE Accounts (
                AccountId     INT IDENTITY(1,1) CONSTRAINT PK_Accounts PRIMARY KEY,
                CustomerId    INT CONSTRAINT FK_Accounts_Customers FOREIGN KEY REFERENCES Customers (CustomerId),
                AccountNumber NVARCHAR(20),
                Balance       DECIMAL(18,2)
            )
            """;
        Execute(createAccounts);

        Execute("""
            INSERT INTO Customers (FullName, Email) VALUES
                ('Asha Rao',   'asha@example.com'),
                ('Ben Carter', 'ben@example.com'),
                ('Chen Li',    'chen@example.com')
            """);

        // One customer can own many accounts (Asha has two): that is a one-to-many relationship.
        // Each account row names one owner. Chen has no account yet, which is allowed.
        Execute("""
            INSERT INTO Accounts (CustomerId, AccountNumber, Balance) VALUES
                (1, 'ACC-1001', 1200.00),
                (1, 'ACC-1002',  300.00),
                (2, 'ACC-2001',   50.00)
            """);

        // JOIN (Topic 11) glues each account to its owner by matching Accounts.CustomerId = Customers.CustomerId.
        Console.WriteLine("Each account joined to its owner:");
        Query("""
            SELECT c.FullName, a.AccountNumber, a.Balance
            FROM Accounts a
            JOIN Customers c ON c.CustomerId = a.CustomerId
            ORDER BY a.AccountId
            """);

        // The foreign key protects the link from both sides (error 547 both times).
        Console.WriteLine("1) An account for a customer that does not exist:");
        Refused("INSERT INTO Accounts (CustomerId, AccountNumber, Balance) VALUES (99, 'ACC-9999', 0)");

        Console.WriteLine("2) Deleting a customer who still owns accounts:");
        Refused("DELETE FROM Customers WHERE CustomerId = 1");

        // A parent with no children can be deleted: nothing points at Chen.
        int deleted = Execute("DELETE FROM Customers WHERE FullName = 'Chen Li'");
        Console.WriteLine($"Deleting Chen (no accounts) worked: {deleted} row deleted.");
    }

    // 4. Constraints are rules the database enforces for every writer; NULL means "no value".
    public void ExplainConstraintsAndNulls()
    {
        PrintHeading("4. Constraints and NULL — rules the database enforces");
        DropAllTables();

        // The full bank tables. Read each column as: name, type, then its rules.
        //   NOT NULL  - a value is required.      UNIQUE  - no two rows may share it.
        //   CHECK     - the value must pass a test. DEFAULT - used when the INSERT leaves the column out.
        Execute("""
            CREATE TABLE Customers (
                CustomerId INT IDENTITY(1,1) CONSTRAINT PK_Customers PRIMARY KEY,
                FullName   NVARCHAR(100) NOT NULL,
                Email      NVARCHAR(200) NOT NULL CONSTRAINT UQ_Customers_Email UNIQUE,
                CreatedAt  DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME()
            )
            """);
        Execute("""
            CREATE TABLE Accounts (
                AccountId     INT IDENTITY(1,1) CONSTRAINT PK_Accounts PRIMARY KEY,
                CustomerId    INT           NOT NULL CONSTRAINT FK_Accounts_Customers FOREIGN KEY REFERENCES Customers (CustomerId),
                AccountNumber NVARCHAR(20)  NOT NULL CONSTRAINT UQ_Accounts_AccountNumber UNIQUE,
                AccountType   NVARCHAR(20)  NOT NULL CONSTRAINT CK_Accounts_AccountType CHECK (AccountType IN ('Savings', 'Current')),
                Balance       DECIMAL(18,2) NOT NULL DEFAULT 0
            )
            """);

        // DEFAULT at work: CreatedAt and Balance are left out of the INSERTs and still get a value.
        Execute("INSERT INTO Customers (FullName, Email) VALUES ('Asha Rao', 'asha@example.com')");
        Execute("INSERT INTO Accounts (CustomerId, AccountNumber, AccountType) VALUES (1, 'ACC-1001', 'Savings')");
        Query("""
            SELECT c.FullName, a.AccountNumber, a.Balance,
                   CASE WHEN c.CreatedAt IS NULL THEN 'missing' ELSE 'filled in by DEFAULT' END AS CreatedAt
            FROM Customers c JOIN Accounts a ON a.CustomerId = c.CustomerId
            """);

        // Each rule rejects a bad row before it is stored.
        Console.WriteLine("UNIQUE: a second customer with the same email (error 2627):");
        Refused("INSERT INTO Customers (FullName, Email) VALUES ('Another Asha', 'asha@example.com')");
        Console.WriteLine("NOT NULL: a customer with no name (error 515):");
        Refused("INSERT INTO Customers (FullName, Email) VALUES (NULL, 'noname@example.com')");
        Console.WriteLine("CHECK: an account type that is not Savings or Current (error 547):");
        Refused("INSERT INTO Accounts (CustomerId, AccountNumber, AccountType) VALUES (1, 'ACC-1002', 'Gold')");

        ShowWhatNullMeans();
    }

    // 5. Normalization: store each fact once. A flat table repeats facts; three linked tables do not.
    public void ExplainNormalization()
    {
        PrintHeading("5. Normalization — store each fact in one place");
        DropAllTables();

        // First try: everything in ONE table, one row per transaction. It looks simple.
        Execute("""
            CREATE TABLE BankFlat (
                RowId           INT IDENTITY(1,1) PRIMARY KEY,
                CustomerName    NVARCHAR(100),
                CustomerEmail   NVARCHAR(200),
                AccountNumber   NVARCHAR(20),
                AccountType     NVARCHAR(20),
                TransactionKind NVARCHAR(10),
                Amount          DECIMAL(18,2)
            )
            """);
        Execute("""
            INSERT INTO BankFlat (CustomerName, CustomerEmail, AccountNumber, AccountType, TransactionKind, Amount) VALUES
                ('Asha Rao',   'asha@example.com', 'ACC-1001', 'Savings', 'Deposit',  1000.00),
                ('Asha Rao',   'asha@example.com', 'ACC-1001', 'Savings', 'Deposit',   200.00),
                ('Asha Rao',   'asha@example.com', 'ACC-1002', 'Current', 'Deposit',   500.00),
                ('Asha Rao',   'asha@example.com', 'ACC-1002', 'Current', 'Withdrawal', 200.00),
                ('Ben Carter', 'ben@example.com',  'ACC-2001', 'Current', 'Deposit',    50.00)
            """);
        Console.WriteLine("The flat table: Asha's name and email are copied onto every one of her rows.");
        Query("SELECT RowId, CustomerName, CustomerEmail, AccountNumber, TransactionKind, Amount FROM BankFlat ORDER BY RowId");

        // The update anomaly: one real-world change (Asha's new email) needs FOUR row edits. Miss one
        // and the table now disagrees with itself, and nothing in the database can tell which is right.
        Console.WriteLine("Asha changes her email, but we only fix row 1 (an update anomaly):");
        Execute("UPDATE BankFlat SET CustomerEmail = 'asha.rao@newmail.example' WHERE RowId = 1");
        Query("SELECT DISTINCT CustomerName, CustomerEmail FROM BankFlat WHERE CustomerName = 'Asha Rao' ORDER BY CustomerEmail");

        // The fix is to split by WHO the fact is about: people, accounts, transactions, linked by keys.
        Execute("DROP TABLE BankFlat");
        CreateBankTables();
        SeedBankRows();

        // The same change now touches exactly one row, because Asha's email is stored once.
        Console.WriteLine("Normalized: the same email change is one UPDATE on one row:");
        int changed = Execute("UPDATE Customers SET Email = 'asha.rao@newmail.example' WHERE FullName = 'Asha Rao'");
        Console.WriteLine($"{changed} row changed.");
        Query("SELECT CustomerId, FullName, Email FROM Customers ORDER BY CustomerId");

        // Nothing was lost: joining the three tables rebuilds the flat picture on demand.
        Console.WriteLine("Joining the three tables gives the flat view back, always consistent:");
        Query("""
            SELECT c.FullName, c.Email, a.AccountNumber, t.Kind, t.Amount
            FROM Transactions t
            JOIN Accounts  a ON a.AccountId  = t.AccountId
            JOIN Customers c ON c.CustomerId = a.CustomerId
            ORDER BY t.TransactionId
            """);
    }

    // NULL is "no value / unknown". It is not 0 and not an empty string, and it does not equal anything,
    // not even another NULL. We add one optional column to show it (Phone may be left empty).
    private void ShowWhatNullMeans()
    {
        Console.WriteLine("NULL means 'no value'. Add an OPTIONAL Phone column (no NOT NULL):");
        Execute("ALTER TABLE Customers ADD Phone NVARCHAR(20) NULL");
        Execute("""
            INSERT INTO Customers (FullName, Email, Phone) VALUES
                ('Ben Carter', 'ben@example.com', NULL),
                ('Chen Li',    'chen@example.com', '')
            """);
        Execute("UPDATE Customers SET Phone = '555-0101' WHERE FullName = 'Asha Rao'");

        // Asha has a phone number, Ben has NULL (never given), Chen has an empty string (given, but blank).
        Query("SELECT FullName, Phone, LEN(Phone) AS PhoneLength FROM Customers ORDER BY CustomerId");

        // Comparing with = never matches NULL; the special test IS NULL does.
        Console.WriteLine("WHERE Phone = NULL finds nobody:");
        Query("SELECT FullName FROM Customers WHERE Phone = NULL");
        Console.WriteLine("WHERE Phone IS NULL finds Ben:");
        Query("SELECT FullName FROM Customers WHERE Phone IS NULL");
        Console.WriteLine("WHERE Phone = '' finds Chen (empty text is a value, NULL is not):");
        Query("SELECT FullName FROM Customers WHERE Phone = ''");
    }

    // The three bank tables with every rule from method 4. Parents first, because a foreign key needs
    // its parent table to exist already.
    private void CreateBankTables()
    {
        Execute("""
            CREATE TABLE Customers (
                CustomerId INT IDENTITY(1,1) CONSTRAINT PK_Customers PRIMARY KEY,
                FullName   NVARCHAR(100) NOT NULL,
                Email      NVARCHAR(200) NOT NULL CONSTRAINT UQ_Customers_Email UNIQUE,
                CreatedAt  DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME()
            )
            """);
        Execute("""
            CREATE TABLE Accounts (
                AccountId     INT IDENTITY(1,1) CONSTRAINT PK_Accounts PRIMARY KEY,
                CustomerId    INT           NOT NULL CONSTRAINT FK_Accounts_Customers FOREIGN KEY REFERENCES Customers (CustomerId),
                AccountNumber NVARCHAR(20)  NOT NULL CONSTRAINT UQ_Accounts_AccountNumber UNIQUE,
                AccountType   NVARCHAR(20)  NOT NULL CONSTRAINT CK_Accounts_AccountType CHECK (AccountType IN ('Savings', 'Current')),
                Balance       DECIMAL(18,2) NOT NULL DEFAULT 0
            )
            """);
        Execute("""
            CREATE TABLE Transactions (
                TransactionId BIGINT IDENTITY(1,1) CONSTRAINT PK_Transactions PRIMARY KEY,
                AccountId     INT           NOT NULL CONSTRAINT FK_Transactions_Accounts FOREIGN KEY REFERENCES Accounts (AccountId),
                Amount        DECIMAL(18,2) NOT NULL,
                Kind          NVARCHAR(10)  NOT NULL CONSTRAINT CK_Transactions_Kind CHECK (Kind IN ('Deposit', 'Withdrawal')),
                CreatedAt     DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME()
            )
            """);
    }

    // The same facts as the flat table, stored once each. Balances match the transactions below.
    // (Balance is a stored total, derived from Transactions, kept here for simplicity. Keeping such a copy
    // in step is a real design trade-off, covered later.)
    private void SeedBankRows()
    {
        Execute("""
            INSERT INTO Customers (FullName, Email) VALUES
                ('Asha Rao',   'asha@example.com'),
                ('Ben Carter', 'ben@example.com'),
                ('Chen Li',    'chen@example.com')
            """);
        Execute("""
            INSERT INTO Accounts (CustomerId, AccountNumber, AccountType, Balance) VALUES
                (1, 'ACC-1001', 'Savings', 1200.00),
                (1, 'ACC-1002', 'Current',  300.00),
                (2, 'ACC-2001', 'Current',   50.00)
            """);
        Execute("""
            INSERT INTO Transactions (AccountId, Amount, Kind) VALUES
                (1, 1000.00, 'Deposit'),
                (1,  200.00, 'Deposit'),
                (2,  500.00, 'Deposit'),
                (2,  200.00, 'Withdrawal'),
                (3,   50.00, 'Deposit')
            """);
    }

    // Children before parents: a table that others point at cannot be dropped while they exist.
    private void DropAllTables()
    {
        Console.WriteLine("(Starting fresh: dropping the lesson's tables if they exist.)");
        SqlLab.Run("""
            IF OBJECT_ID('dbo.Transactions', 'U') IS NOT NULL DROP TABLE dbo.Transactions;
            IF OBJECT_ID('dbo.Accounts',     'U') IS NOT NULL DROP TABLE dbo.Accounts;
            IF OBJECT_ID('dbo.Customers',    'U') IS NOT NULL DROP TABLE dbo.Customers;
            IF OBJECT_ID('dbo.BankFlat',     'U') IS NOT NULL DROP TABLE dbo.BankFlat;
            """);
    }

    // Prints a statement, runs it, and returns how many rows it changed (-1 for CREATE and similar).
    private static int Execute(string sql)
    {
        PrintSql(sql);
        return SqlLab.Run(sql);
    }

    // Prints a SELECT, runs it, and prints the rows it returns as a small grid.
    private static void Query(string sql)
    {
        PrintSql(sql);
        SqlLab.Show(sql);
    }

    // Prints a statement that SQL Server is EXPECTED to reject, then the error number it reports.
    private static void Refused(string sql)
    {
        PrintSql(sql);
        SqlLab.ShowError(sql);
    }

    // Shows the T-SQL being sent, indented, so the learner sees exactly what produced the output.
    private static void PrintSql(string sql)
    {
        foreach (string line in sql.Split('\n'))
        {
            Console.WriteLine("  sql> " + line.TrimEnd());
        }
    }

    private static void PrintHeading(string title)
    {
        Console.WriteLine();
        Console.WriteLine($"=== {title} ===");
    }
}
