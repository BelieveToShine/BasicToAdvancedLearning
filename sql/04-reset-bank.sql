/*
  04-reset-bank.sql  -- DESTRUCTIVE: drops dbo.Transactions, dbo.Accounts and dbo.Customers (children first) in the
  CURRENT database. Use it to start a manual experiment from zero:
      sqlcmd ... -b -d <ScratchDatabase> -i sql/04-reset-bank.sql
      sqlcmd ... -b -d <ScratchDatabase> -i sql/01-bank-schema.sql -i sql/02-bank-seed.sql
  Safety: refuses to run in master, model, msdb or tempdb, and ONLY runs in a database whose name looks like a scratch
  database (contains 'scratch', 'test' or 'e2e', or starts with 'BasicToAdvancedLearning_'), so it cannot drop the tables
  of a real application database that happens to have the same table names. It prints what it is about to drop first.
*/
SET NOCOUNT ON;
IF DB_NAME() IN (N'master', N'model', N'msdb', N'tempdb')
    THROW 50002, N'refusing to drop tables in a system database', 1;
IF NOT (DB_NAME() LIKE N'%scratch%' OR DB_NAME() LIKE N'%test%' OR DB_NAME() LIKE N'%e2e%' OR DB_NAME() LIKE N'BasicToAdvancedLearning[_]%')
    THROW 50003, N'refusing: this does not look like a scratch database (name must contain scratch, test or e2e, or start with BasicToAdvancedLearning_). Create one with CREATE DATABASE bank_scratch and run the script there.', 1;
PRINT N'about to drop dbo.Transactions, dbo.Accounts, dbo.Customers in database ' + DB_NAME();

DROP TABLE IF EXISTS dbo.Transactions;
DROP TABLE IF EXISTS dbo.Accounts;
DROP TABLE IF EXISTS dbo.Customers;
PRINT N'bank tables dropped in ' + DB_NAME();
