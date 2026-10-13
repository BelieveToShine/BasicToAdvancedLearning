/*
  03-verify-bank-schema.sql  -- read-only self-check. Prints one PASS/FAIL line per check and FAILS the run
  (THROW => non-zero exit code when sqlcmd is started with -b) if any check fails.

  Checks: the three tables, every named constraint from sql/01-bank-schema.sql, and (if sql/02-bank-seed.sql ran)
  that each stored Balance equals the sum of its transactions.
  Run:  sqlcmd -S <server> -U <login> -P <password> -C -b -d <ScratchDatabase> -i sql/03-verify-bank-schema.sql
*/
SET NOCOUNT ON;
DECLARE @Results TABLE (Seq INT IDENTITY(1,1), Name NVARCHAR(120), Ok BIT);

INSERT @Results (Name, Ok) VALUES
    (N'table dbo.Customers',    IIF(OBJECT_ID(N'dbo.Customers',    N'U') IS NOT NULL, 1, 0)),
    (N'table dbo.Accounts',     IIF(OBJECT_ID(N'dbo.Accounts',     N'U') IS NOT NULL, 1, 0)),
    (N'table dbo.Transactions', IIF(OBJECT_ID(N'dbo.Transactions', N'U') IS NOT NULL, 1, 0));

DECLARE @Constraints TABLE (Name sysname, Kind NVARCHAR(40));
INSERT @Constraints (Name, Kind) VALUES
    (N'PK_Customers', N'primary key'), (N'UQ_Customers_Email', N'unique'), (N'DF_Customers_CreatedAt', N'default'),
    (N'PK_Accounts', N'primary key'), (N'FK_Accounts_Customers', N'foreign key'), (N'UQ_Accounts_AccountNumber', N'unique'),
    (N'CK_Accounts_AccountType', N'check'), (N'DF_Accounts_Balance', N'default'),
    (N'PK_Transactions', N'primary key'), (N'FK_Transactions_Accounts', N'foreign key'),
    (N'CK_Transactions_Kind', N'check'), (N'DF_Transactions_CreatedAt', N'default');

INSERT @Results (Name, Ok)
SELECT N'constraint ' + c.Name + N' (' + c.Kind + N')',
       IIF(EXISTS (SELECT 1 FROM sys.objects o WHERE o.name = c.Name AND o.schema_id = SCHEMA_ID(N'dbo')), 1, 0)
FROM @Constraints AS c;

-- Only meaningful when the tables exist AND hold the seed rows.
IF OBJECT_ID(N'dbo.Accounts', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Transactions', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM dbo.Transactions)
BEGIN
    INSERT @Results (Name, Ok)
    SELECT N'balance of ' + a.AccountNumber + N' = sum of its transactions',
           IIF(a.Balance = COALESCE((SELECT SUM(IIF(t.Kind = N'Deposit', t.Amount, -t.Amount))
                                     FROM dbo.Transactions AS t WHERE t.AccountId = a.AccountId), 0), 1, 0)
    FROM dbo.Accounts AS a;
END

SELECT IIF(Ok = 1, N'PASS', N'FAIL') AS Result, Name FROM @Results ORDER BY Seq;

IF EXISTS (SELECT 1 FROM @Results WHERE Ok = 0)
    THROW 50001, N'bank schema verification FAILED - see the FAIL rows above', 1;
PRINT N'ALL CHECKS PASSED';
