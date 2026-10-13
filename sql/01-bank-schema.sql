/*
  01-bank-schema.sql  -- idempotent, NON-destructive (creates only what is missing).

  What: the reference "bank" schema that the SQL lessons (topics 10-15, 18) are built on:
        Customers -> Accounts -> Transactions. Run it in ANY scratch database to poke at the tables by hand
        (the lessons themselves create their own copies in their own databases; they do not need this script).
  Run:  sqlcmd -S <server> -U <login> -P <password> -C -b -d <ScratchDatabase> -i sql/01-bank-schema.sql
        then sql/02-bank-seed.sql and sql/03-verify-bank-schema.sql.
  Note: constraint names are fixed on purpose, so error messages and the verify script are repeatable.
*/
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
    CREATE TABLE dbo.Customers
    (
        CustomerId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
        FullName   NVARCHAR(100)     NOT NULL,
        Email      NVARCHAR(200)     NOT NULL CONSTRAINT UQ_Customers_Email UNIQUE,
        CreatedAt  DATETIME2         NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT SYSUTCDATETIME()
    );

IF OBJECT_ID(N'dbo.Accounts', N'U') IS NULL
    CREATE TABLE dbo.Accounts
    (
        AccountId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Accounts PRIMARY KEY,
        CustomerId    INT               NOT NULL CONSTRAINT FK_Accounts_Customers REFERENCES dbo.Customers (CustomerId),
        AccountNumber NVARCHAR(20)      NOT NULL CONSTRAINT UQ_Accounts_AccountNumber UNIQUE,
        AccountType   NVARCHAR(20)      NOT NULL CONSTRAINT CK_Accounts_AccountType CHECK (AccountType IN (N'Savings', N'Current')),
        Balance       DECIMAL(18,2)     NOT NULL CONSTRAINT DF_Accounts_Balance DEFAULT 0
    );

IF OBJECT_ID(N'dbo.Transactions', N'U') IS NULL
    CREATE TABLE dbo.Transactions
    (
        TransactionId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Transactions PRIMARY KEY,
        AccountId     INT                  NOT NULL CONSTRAINT FK_Transactions_Accounts REFERENCES dbo.Accounts (AccountId),
        Amount        DECIMAL(18,2)        NOT NULL,
        Kind          NVARCHAR(10)         NOT NULL CONSTRAINT CK_Transactions_Kind CHECK (Kind IN (N'Deposit', N'Withdrawal')),
        CreatedAt     DATETIME2            NOT NULL CONSTRAINT DF_Transactions_CreatedAt DEFAULT SYSUTCDATETIME()
    );

PRINT N'bank schema ready: dbo.Customers, dbo.Accounts, dbo.Transactions';
