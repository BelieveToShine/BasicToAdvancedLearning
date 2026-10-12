IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012175006_InitialCreate'
)
BEGIN
    CREATE TABLE [Customers] (
        [CustomerId] int NOT NULL IDENTITY,
        [FullName] nvarchar(100) NOT NULL,
        [Email] nvarchar(200) NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Customers] PRIMARY KEY ([CustomerId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012175006_InitialCreate'
)
BEGIN
    CREATE TABLE [Accounts] (
        [AccountId] int NOT NULL IDENTITY,
        [CustomerId] int NOT NULL,
        [AccountNumber] nvarchar(20) NOT NULL,
        [AccountType] nvarchar(20) NOT NULL,
        [Balance] decimal(18,2) NOT NULL DEFAULT 0.0,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Accounts] PRIMARY KEY ([AccountId]),
        CONSTRAINT [CK_Accounts_AccountType] CHECK ([AccountType] IN ('Savings','Current')),
        CONSTRAINT [CK_Accounts_Balance_NotNegative] CHECK ([Balance] >= 0),
        CONSTRAINT [FK_Accounts_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([CustomerId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012175006_InitialCreate'
)
BEGIN
    CREATE TABLE [Transactions] (
        [TransactionId] bigint NOT NULL IDENTITY,
        [AccountId] int NOT NULL,
        [Kind] nvarchar(10) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Transactions] PRIMARY KEY ([TransactionId]),
        CONSTRAINT [CK_Transactions_Amount_Positive] CHECK ([Amount] > 0),
        CONSTRAINT [CK_Transactions_Kind] CHECK ([Kind] IN ('Deposit','Withdrawal')),
        CONSTRAINT [FK_Transactions_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Accounts] ([AccountId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012175006_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Accounts_AccountNumber] ON [Accounts] ([AccountNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012175006_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Accounts_CustomerId] ON [Accounts] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012175006_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Customers_Email] ON [Customers] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012175006_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Transactions_AccountId] ON [Transactions] ([AccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012175006_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261012175006_InitialCreate', N'9.0.20');
END;

COMMIT;
GO

