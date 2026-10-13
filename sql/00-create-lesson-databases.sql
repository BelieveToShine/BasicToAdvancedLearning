/*
  00-create-lesson-databases.sql  -- OPTIONAL, idempotent, safe to re-run.

  What: creates the scratch databases the lessons use, so a login WITHOUT the CREATE DATABASE permission can still
        run the lessons (a DBA runs this once). Lessons create their own database automatically when the login
        may; this script only exists for locked-down servers and for people who want to see the names up front.

  Names: <DbBase>_<LessonFolder>   e.g. BasicToAdvancedLearning_SqlCrud
         (the same rule as Interfaces/SqlLab.cs: the base is the "Initial Catalog" of BTAL_SQLSERVER, the suffix is
          the lesson's folder name). Keep @DbBase equal to the Initial Catalog you put in BTAL_SQLSERVER.

  Run:   sqlcmd -S <server> -U <login> -P <password> -C -b -i sql/00-create-lesson-databases.sql
         (or paste into SSMS / Azure Data Studio). Edit @DbBase below first if your Initial Catalog differs.
  Needs: CREATE DATABASE permission (dbcreator or sysadmin). Nothing else is touched.
*/
SET NOCOUNT ON;
DECLARE @DbBase sysname = N'BasicToAdvancedLearning';   -- <-- must match "Initial Catalog" in BTAL_SQLSERVER

DECLARE @Lessons TABLE (Folder sysname PRIMARY KEY);
INSERT @Lessons (Folder) VALUES
    (N'RelationalConcepts'), (N'SqlBasics'), (N'SqlCrud'),            -- topics 10-12
    (N'AdoNet'), (N'EfCoreBasics'), (N'EfCoreCrud'),                  -- topics 13-15
    (N'WebApiCrud'),                                                  -- topic 18 (the only web lesson that uses SQL)
    (N'FullStackBank');                                               -- the real API in FullStackBank/Api (topics 25-28)

DECLARE @Folder sysname, @Name sysname, @Sql nvarchar(max);
DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT Folder FROM @Lessons ORDER BY Folder;
OPEN c;
FETCH NEXT FROM c INTO @Folder;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @Name = @DbBase + N'_' + @Folder;
    IF DB_ID(@Name) IS NULL
    BEGIN
        SET @Sql = N'CREATE DATABASE ' + QUOTENAME(@Name);
        EXEC (@Sql);
        PRINT N'created  ' + @Name;
    END
    ELSE PRINT N'exists   ' + @Name;
    FETCH NEXT FROM c INTO @Folder;
END
CLOSE c; DEALLOCATE c;
