/*
  99-drop-lesson-databases.sql  -- DESTRUCTIVE cleanup of the scratch databases created by the lessons / 00-script.

  Safety: does NOTHING unless you change @Confirm to N'YES'. Only databases named  <@DbBase>_<one of the lesson folders>
  are touched (never a database that merely starts with the base name). Open connections are kicked out first.
  Run:  edit @Confirm, then  sqlcmd -S <server> -U <login> -P <password> -C -b -i sql/99-drop-lesson-databases.sql
*/
SET NOCOUNT ON;
DECLARE @Confirm nvarchar(3) = N'NO';                      -- <-- change to N'YES' to really drop
DECLARE @DbBase  sysname     = N'BasicToAdvancedLearning'; -- <-- same value as in 00-create-lesson-databases.sql

DECLARE @Lessons TABLE (Folder sysname PRIMARY KEY);
INSERT @Lessons (Folder) VALUES
    (N'RelationalConcepts'), (N'SqlBasics'), (N'SqlCrud'), (N'AdoNet'), (N'EfCoreBasics'), (N'EfCoreCrud'),
    (N'WebApiCrud'), (N'FullStackBank');

DECLARE @Name sysname, @Sql nvarchar(max);
DECLARE c CURSOR LOCAL FAST_FORWARD FOR
    SELECT @DbBase + N'_' + Folder FROM @Lessons ORDER BY Folder;
OPEN c;
FETCH NEXT FROM c INTO @Name;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF DB_ID(@Name) IS NOT NULL
    BEGIN
        IF @Confirm = N'YES'
        BEGIN
            SET @Sql = N'ALTER DATABASE ' + QUOTENAME(@Name) + N' SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE ' + QUOTENAME(@Name);
            EXEC (@Sql);
            PRINT N'dropped  ' + @Name;
        END
        ELSE PRINT N'would drop (set @Confirm = N''YES'')  ' + @Name;
    END
    FETCH NEXT FROM c INTO @Name;
END
CLOSE c; DEALLOCATE c;
