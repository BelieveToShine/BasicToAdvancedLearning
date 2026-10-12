-- Step 1 of the database set-up: create the (empty) database if it is missing.
-- Run by db/init.sh with sqlcmd, connected to "master". $(DB_NAME) is a sqlcmd variable (-v DB_NAME=...), filled
-- in by init.sh, so the name lives in ONE place (.env). Safe to run again: it only creates the database when it is absent.
IF DB_ID(N'$(DB_NAME)') IS NULL
    CREATE DATABASE [$(DB_NAME)];
GO
