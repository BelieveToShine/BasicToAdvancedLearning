-- Step 3: the login the API uses. It is NOT "sa".
-- The API only reads and writes rows (SELECT/INSERT/UPDATE/DELETE), so it gets exactly that: db_datareader +
-- db_datawriter. It cannot create or drop tables - the schema is changed only by the deployment step (01-schema.sql),
-- run here by init.sh with the admin login. Run by init.sh connected to master (the script switches to the bank database with USE); safe to run again.
-- The password policy stays ON (SQL Server default), so a weak APP_DB_PASSWORD is rejected. If the login already exists,
-- ALTER LOGIN applies the password from .env, so changing APP_DB_PASSWORD and running `docker compose up` again works.
-- $(APP_USER) and $(APP_PASSWORD) are sqlcmd variables filled in from .env.
USE [$(DB_NAME)];
GO
IF SUSER_ID(N'$(APP_USER)') IS NULL
    CREATE LOGIN [$(APP_USER)] WITH PASSWORD = N'$(APP_PASSWORD)';
ELSE
    ALTER LOGIN [$(APP_USER)] WITH PASSWORD = N'$(APP_PASSWORD)';
GO
IF USER_ID(N'$(APP_USER)') IS NULL
    CREATE USER [$(APP_USER)] FOR LOGIN [$(APP_USER)];
GO
ALTER ROLE db_datareader ADD MEMBER [$(APP_USER)];
ALTER ROLE db_datawriter ADD MEMBER [$(APP_USER)];
GO
