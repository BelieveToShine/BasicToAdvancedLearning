/*
  02-bank-seed.sql  -- idempotent: inserts the sample rows ONLY into tables that are still empty.

  Same people as the lessons: Asha Rao, Ben Carter, Chen Li (+ Dana Smith, who has no account, so LEFT JOIN is interesting).
  Balances equal the sum of each account's transactions (deposits minus withdrawals).
  Run after sql/01-bank-schema.sql:  sqlcmd ... -b -d <ScratchDatabase> -i sql/02-bank-seed.sql
*/
SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM dbo.Customers)
BEGIN
    INSERT dbo.Customers (FullName, Email) VALUES
        (N'Asha Rao',   N'asha@example.com'),
        (N'Ben Carter', N'ben@example.com'),
        (N'Chen Li',    N'chen@example.com'),
        (N'Dana Smith', N'dana@example.com');
END

IF NOT EXISTS (SELECT 1 FROM dbo.Accounts)
BEGIN
    INSERT dbo.Accounts (CustomerId, AccountNumber, AccountType, Balance)
    SELECT c.CustomerId, v.AccountNumber, v.AccountType, v.Balance
    FROM (VALUES
        (N'asha@example.com', N'ACC-1001', N'Savings', CONVERT(DECIMAL(18,2), 1200.00)),
        (N'asha@example.com', N'ACC-1002', N'Current',            300.00),
        (N'ben@example.com',  N'ACC-2001', N'Savings',            50.00),
        (N'chen@example.com', N'ACC-3001', N'Current',            0.00)
    ) AS v (Email, AccountNumber, AccountType, Balance)
    JOIN dbo.Customers AS c ON c.Email = v.Email;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Transactions)
BEGIN
    INSERT dbo.Transactions (AccountId, Amount, Kind)
    SELECT a.AccountId, v.Amount, v.Kind
    FROM (VALUES
        (N'ACC-1001', CONVERT(DECIMAL(18,2), 1000.00), N'Deposit'),
        (N'ACC-1001',                         200.00,  N'Deposit'),
        (N'ACC-1002',                         500.00,  N'Deposit'),
        (N'ACC-1002',                         200.00,  N'Withdrawal'),
        (N'ACC-2001',                          50.00,  N'Deposit')
    ) AS v (AccountNumber, Amount, Kind)
    JOIN dbo.Accounts AS a ON a.AccountNumber = v.AccountNumber;
END

SELECT (SELECT COUNT(*) FROM dbo.Customers)    AS Customers,
       (SELECT COUNT(*) FROM dbo.Accounts)     AS Accounts,
       (SELECT COUNT(*) FROM dbo.Transactions) AS Transactions;
