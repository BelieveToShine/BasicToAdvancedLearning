using BasicToAdvancedLearning.Interfaces;

namespace BasicToAdvancedLearning.SqlCrud;

// Helper for Topic 12 (not a lesson): drops and recreates the three bank tables, EMPTY, so every run of
// the lesson starts from the same place. The lesson itself (SqlCrudDemo) then fills them with INSERT.
// Designing tables (CREATE TABLE, keys) is not this lesson's subject, so it is
// only skimmed here. Every constraint has a name on purpose:
// SQL Server would otherwise invent a random-looking one, and the error messages in this lesson would
// read differently on every run.
internal static class SqlCrudSetup
{
    // Drops the tables (children first: a table that others point to cannot be dropped while
    // the pointers exist) and creates them again, empty.
    public static void CreateBankTables()
    {
        SqlLab.Run("""
            IF OBJECT_ID(N'dbo.Transactions', N'U') IS NOT NULL DROP TABLE dbo.Transactions;
            IF OBJECT_ID(N'dbo.Accounts', N'U') IS NOT NULL DROP TABLE dbo.Accounts;
            IF OBJECT_ID(N'dbo.Customers', N'U') IS NOT NULL DROP TABLE dbo.Customers;
            IF OBJECT_ID(N'dbo.AccountsScratch', N'U') IS NOT NULL DROP TABLE dbo.AccountsScratch;
            """);

        SqlLab.Run("""
            CREATE TABLE dbo.Customers (
                CustomerId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
                FullName   NVARCHAR(100) NOT NULL,
                Email      NVARCHAR(200) NOT NULL CONSTRAINT UQ_Customers_Email UNIQUE,
                CreatedAt  DATETIME2 NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT SYSUTCDATETIME()
            );
            CREATE TABLE dbo.Accounts (
                AccountId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Accounts PRIMARY KEY,
                CustomerId    INT NOT NULL CONSTRAINT FK_Accounts_Customers REFERENCES dbo.Customers (CustomerId),
                AccountNumber NVARCHAR(20) NOT NULL CONSTRAINT UQ_Accounts_AccountNumber UNIQUE,
                AccountType   NVARCHAR(20) NOT NULL CONSTRAINT CK_Accounts_AccountType CHECK (AccountType IN (N'Savings', N'Current')),
                Balance       DECIMAL(18,2) NOT NULL CONSTRAINT DF_Accounts_Balance DEFAULT 0
            );
            CREATE TABLE dbo.Transactions (
                TransactionId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Transactions PRIMARY KEY,
                AccountId     INT NOT NULL CONSTRAINT FK_Transactions_Accounts REFERENCES dbo.Accounts (AccountId),
                Amount        DECIMAL(18,2) NOT NULL,
                Kind          NVARCHAR(10) NOT NULL CONSTRAINT CK_Transactions_Kind CHECK (Kind IN (N'Deposit', N'Withdrawal')),
                CreatedAt     DATETIME2 NOT NULL CONSTRAINT DF_Transactions_CreatedAt DEFAULT SYSUTCDATETIME()
            );
            """);

        Console.WriteLine("Setup: dropped and recreated the empty tables Customers, Accounts and Transactions.");
    }
}
