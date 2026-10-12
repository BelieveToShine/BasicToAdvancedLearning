-- Step 4 (optional): the same three demo customers the Development seed creates, so the deployed stack shows
-- something. Only runs when SEED_DEMO_DATA=true in .env, and only inserts when the Customers table is empty,
-- so running it twice never duplicates rows. A real production database would NOT get demo data.
IF NOT EXISTS (SELECT 1 FROM Customers)
BEGIN
    INSERT INTO Customers (FullName, Email) VALUES
        (N'Asha Rao',   N'asha.rao@example.com'),
        (N'Ben Carter', N'ben.carter@example.com'),
        (N'Chen Li',    N'chen.li@example.com');

    INSERT INTO Accounts (CustomerId, AccountNumber, AccountType, Balance)
    SELECT CustomerId, v.AccountNumber, v.AccountType, v.Balance
    FROM Customers c
    JOIN (VALUES (N'asha.rao@example.com',   N'ACC-1001', N'Savings', 5000.00),
                 (N'ben.carter@example.com', N'ACC-1002', N'Current', 1200.50),
                 (N'chen.li@example.com',    N'ACC-1003', N'Savings',  300.00))
         AS v(Email, AccountNumber, AccountType, Balance) ON v.Email = c.Email;

    -- one opening deposit per account, so balance = sum of its transactions
    INSERT INTO Transactions (AccountId, Kind, Amount)
    SELECT AccountId, N'Deposit', Balance FROM Accounts;
END;
GO
