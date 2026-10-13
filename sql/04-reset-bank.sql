/*
  04-reset-bank.sql  -- DESTRUCTIVE: drops dbo.Transactions, dbo.Accounts and dbo.Customers (children first) in the
  CURRENT database. Use it to start a manual experiment from zero:
      sqlcmd ... -b -d <ScratchDatabase> -i sql/04-reset-bank.sql
      sqlcmd ... -b -d <ScratchDatabase> -i sql/01-bank-schema.sql -i sql/02-bank-seed.sql
  Safety: refuses to run in master, model, msdb or tempdb.
*/
SET NOCOUNT ON;
IF DB_NAME() IN (N'master', N'model', N'msdb', N'tempdb')
    THROW 50002, N'refusing to drop tables in a system database', 1;

DROP TABLE IF EXISTS dbo.Transactions;
DROP TABLE IF EXISTS dbo.Accounts;
DROP TABLE IF EXISTS dbo.Customers;
PRINT N'bank tables dropped in ' + DB_NAME();
