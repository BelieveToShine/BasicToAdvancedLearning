using BasicToAdvancedLearning.Interfaces;

namespace BasicToAdvancedLearning.SqlBasics;

// Helper for Topic 11 (no ExplainX methods — the lesson itself is SqlBasicsDemo). It drops and re-creates the
// three bank tables and fills them with a few memorable rows, so every run starts from the same data and the
// lesson's output is always the same. Dropping a table also restarts its IDENTITY counter, so the generated
// ids are always 1, 2, 3... and the INSERTs below can refer to them by number.
// Topic 10 explains the design of these tables; here we only need them to hold something to query.
internal static class BankSeed
{
    // Drop in this order: Transactions and Accounts point at other tables, so they must go first.
    private const string DropTables = """
        DROP TABLE IF EXISTS Transactions;
        DROP TABLE IF EXISTS Accounts;
        DROP TABLE IF EXISTS Customers;
        """;

    private const string CreateTables = """
        CREATE TABLE Customers (
            CustomerId INT IDENTITY PRIMARY KEY,
            FullName   NVARCHAR(100) NOT NULL,
            Email      NVARCHAR(200) NOT NULL UNIQUE,
            CreatedAt  DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
        );
        CREATE TABLE Accounts (
            AccountId     INT IDENTITY PRIMARY KEY,
            CustomerId    INT NOT NULL REFERENCES Customers (CustomerId),
            AccountNumber NVARCHAR(20) NOT NULL UNIQUE,
            AccountType   NVARCHAR(20) NOT NULL CHECK (AccountType IN ('Savings', 'Current')),
            Balance       DECIMAL(18,2) NOT NULL DEFAULT 0
        );
        CREATE TABLE Transactions (
            TransactionId BIGINT IDENTITY PRIMARY KEY,
            AccountId     INT NOT NULL REFERENCES Accounts (AccountId),
            Amount        DECIMAL(18,2) NOT NULL,
            Kind          NVARCHAR(10) NOT NULL CHECK (Kind IN ('Deposit', 'Withdrawal')),
            CreatedAt     DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
        );
        """;

    // Dana Smith (customer 4) is deliberately given NO account: she is the row a LEFT JOIN keeps and an INNER JOIN drops.
    private const string InsertCustomers = """
        INSERT INTO Customers (FullName, Email) VALUES
            ('Asha Rao',    'asha@example.com'),
            ('Ben Carter',  'ben@example.com'),
            ('Chen Li',     'chen@example.com'),
            ('Dana Smith',  'dana@example.com');
        """;

    // Each balance equals that account's deposits minus its withdrawals in the Transactions rows below
    // (ACC-3002 is brand new: no transactions, balance 0).
    private const string InsertAccounts = """
        INSERT INTO Accounts (CustomerId, AccountNumber, AccountType, Balance) VALUES
            (1, 'ACC-1001', 'Savings', 1500.00),
            (1, 'ACC-1002', 'Current',  250.00),
            (2, 'ACC-2001', 'Savings', 3200.00),
            (2, 'ACC-2002', 'Current',    0.00),
            (3, 'ACC-3001', 'Current',  800.00),
            (3, 'ACC-3002', 'Savings',    0.00);
        """;

    // In our seed data Amount is always positive; Kind says which way the money moved.
    private const string InsertTransactions = """
        INSERT INTO Transactions (AccountId, Amount, Kind) VALUES
            (1, 1000.00, 'Deposit'),
            (1,  700.00, 'Deposit'),
            (1,  200.00, 'Withdrawal'),
            (2,  300.00, 'Deposit'),
            (2,   50.00, 'Withdrawal'),
            (3, 3000.00, 'Deposit'),
            (3,  500.00, 'Deposit'),
            (3,  300.00, 'Withdrawal'),
            (4,  100.00, 'Deposit'),
            (4,  100.00, 'Withdrawal'),
            (5,  900.00, 'Deposit'),
            (5,  100.00, 'Withdrawal');
        """;

    // Rebuilds the tables and prints how many rows went in. Run() returns the number of rows each INSERT added.
    public static void Recreate()
    {
        Console.WriteLine("Setting up the bank tables (dropped and re-created on every run)...");

        SqlLab.Run(DropTables);
        SqlLab.Run(CreateTables);
        int customers = SqlLab.Run(InsertCustomers);
        int accounts = SqlLab.Run(InsertAccounts);
        int transactions = SqlLab.Run(InsertTransactions);

        Console.WriteLine($"Seeded {customers + accounts + transactions} rows: {customers} customers, {accounts} accounts, {transactions} transactions.");
    }
}
