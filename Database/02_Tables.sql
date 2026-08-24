-- GramShopPOS complete schema generated from EF Core migrations (idempotent).
-- Run 01_CreateDatabase.sql first, then this script, then 03-06.
-- Preferred application path: `dotnet ef database update` from the API project.
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
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE TABLE [BusinessSettings] (
        [Id] int NOT NULL IDENTITY,
        [ShopName] nvarchar(200) NOT NULL,
        [LogoPath] nvarchar(max) NULL,
        [Address] nvarchar(max) NULL,
        [Mobile] nvarchar(max) NULL,
        [Email] nvarchar(max) NULL,
        [GSTNumber] nvarchar(max) NULL,
        [InvoiceFooter] nvarchar(max) NULL,
        [ReturnPolicy] nvarchar(max) NULL,
        [InvoicePrefix] nvarchar(max) NOT NULL,
        [InvoiceNumberFormat] nvarchar(max) NOT NULL,
        [FinancialYearStartMonth] int NOT NULL,
        [AllowNegativeStock] bit NOT NULL,
        [DefaultTaxPercent] decimal(5,2) NOT NULL,
        [LowStockDefaultLevel] decimal(18,3) NOT NULL,
        [NewCustomerReward] decimal(18,2) NOT NULL,
        [ReferrerReward] decimal(18,2) NOT NULL,
        [RewardType] int NOT NULL,
        [RewardTrigger] int NOT NULL,
        [ReferralStoreWise] bit NOT NULL,
        [ReferralEnabled] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        [IsDeleted] bit NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_BusinessSettings] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE TABLE [Categories] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        [IsDeleted] bit NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE TABLE [ProductImportBatches] (
        [Id] int NOT NULL IDENTITY,
        [BatchId] uniqueidentifier NOT NULL,
        [UserId] int NOT NULL,
        [ExpiresAtUtc] datetime2 NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [PayloadJson] nvarchar(max) NOT NULL,
        [ValidRowCount] int NOT NULL,
        [ErrorRowCount] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        [IsDeleted] bit NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_ProductImportBatches] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE TABLE [RevokedTokens] (
        [Id] int NOT NULL IDENTITY,
        [Jti] nvarchar(64) NOT NULL,
        [ExpiresAtUtc] datetime2 NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UserId] int NOT NULL,
        CONSTRAINT [PK_RevokedTokens] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE TABLE [Roles] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(50) NOT NULL,
        [Description] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        [IsDeleted] bit NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE TABLE [Stores] (
        [Id] int NOT NULL IDENTITY,
        [StoreCode] nvarchar(20) NOT NULL,
        [StoreName] nvarchar(200) NOT NULL,
        [Address] nvarchar(500) NULL,
        [ContactNumber] nvarchar(20) NULL,
        [GSTNumber] nvarchar(20) NULL,
        [InvoicePrefix] nvarchar(20) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        [IsDeleted] bit NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Stores] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE TABLE [TaxSettings] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(50) NOT NULL,
        [Percent] decimal(5,2) NOT NULL,
        [IsDefault] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        [IsDeleted] bit NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_TaxSettings] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] int NOT NULL IDENTITY,
        [UserName] nvarchar(100) NOT NULL,
        [PasswordHash] nvarchar(500) NOT NULL,
        [FullName] nvarchar(200) NOT NULL,
        [Email] nvarchar(200) NULL,
        [PhoneNumber] nvarchar(20) NULL,
        [MustChangePassword] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        [LockoutEndUtc] datetime2 NULL,
        [LastLoginDate] datetime2 NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        [IsDeleted] bit NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE TABLE [Products] (
        [Id] int NOT NULL IDENTITY,
        [ProductCode] nvarchar(50) NOT NULL,
        [Barcode] nvarchar(50) NULL,
        [ProductName] nvarchar(200) NOT NULL,
        [CategoryId] int NOT NULL,
        [Unit] nvarchar(20) NOT NULL,
        [PurchasePrice] decimal(18,2) NOT NULL,
        [SellingPrice] decimal(18,2) NOT NULL,
        [MRP] decimal(18,2) NOT NULL,
        [TaxPercent] decimal(5,2) NOT NULL,
        [MinimumStockLevel] decimal(18,3) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        [IsDeleted] bit NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Products_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE TABLE [BillSequences] (
        [Id] int NOT NULL IDENTITY,
        [StoreId] int NOT NULL,
        [FinancialYearCode] nvarchar(10) NOT NULL,
        [Prefix] nvarchar(20) NOT NULL,
        [LastNumber] int NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        [IsDeleted] bit NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_BillSequences] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BillSequences_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE TABLE [Customers] (
        [Id] int NOT NULL IDENTITY,
        [StoreId] int NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [MobileNumber] nvarchar(20) NOT NULL,
        [Address] nvarchar(500) NULL,
        [ReferralCode] nvarchar(20) NOT NULL,
        [ReferredByCustomerId] int NULL,
        [OutstandingBalance] decimal(18,2) NOT NULL,
        [WalletBalance] decimal(18,2) NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        [IsDeleted] bit NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Customers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Customers_Customers_ReferredByCustomerId] FOREIGN KEY ([ReferredByCustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Customers_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE TABLE [ReturnSequences] (
        [Id] int NOT NULL IDENTITY,
        [StoreId] int NOT NULL,
        [FinancialYearCode] nvarchar(10) NOT NULL,
        [Prefix] nvarchar(20) NOT NULL,
        [LastNumber] int NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        [IsDeleted] bit NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_ReturnSequences] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ReturnSequences_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [UserId] int NULL,
        [StoreId] int NULL,
        [Action] nvarchar(100) NOT NULL,
        [EntityName] nvarchar(100) NOT NULL,
        [EntityId] nvarchar(50) NULL,
        [OldValue] nvarchar(max) NULL,
        [NewValue] nvarchar(max) NULL,
        [IpAddress] nvarchar(50) NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AuditLogs_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AuditLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE TABLE [PasswordResetTokens] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [TokenHash] nvarchar(500) NOT NULL,
        [ExpiresAtUtc] datetime2 NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [IsUsed] bit NOT NULL,
        CONSTRAINT [PK_PasswordResetTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PasswordResetTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE TABLE [Purchases] (
        [Id] int NOT NULL IDENTITY,
        [StoreId] int NOT NULL,
        [SupplierName] nvarchar(200) NOT NULL,
        [InvoiceNumber] nvarchar(50) NOT NULL,
        [PurchaseDate] datetime2 NOT NULL,
        [Total] decimal(18,2) NOT NULL,
        [Notes] nvarchar(max) NULL,
        [UserId] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        [IsDeleted] bit NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Purchases] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Purchases_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Purchases_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE TABLE [StockTransfers] (
        [Id] int NOT NULL IDENTITY,
        [TransferNumber] nvarchar(50) NOT NULL,
        [FromStoreId] int NOT NULL,
        [ToStoreId] int NOT NULL,
        [TransferDate] datetime2 NOT NULL,
        [Status] int NOT NULL,
        [Reason] nvarchar(max) NULL,
        [UserId] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        [IsDeleted] bit NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_StockTransfers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StockTransfers_Stores_FromStoreId] FOREIGN KEY ([FromStoreId]) REFERENCES [Stores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockTransfers_Stores_ToStoreId] FOREIGN KEY ([ToStoreId]) REFERENCES [Stores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockTransfers_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE TABLE [StoreUsers] (
        [StoreId] int NOT NULL,
        [UserId] int NOT NULL,
        [IsPrimary] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_StoreUsers] PRIMARY KEY ([StoreId], [UserId]),
        CONSTRAINT [FK_StoreUsers_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StoreUsers_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE TABLE [UserRoles] (
        [UserId] int NOT NULL,
        [RoleId] int NOT NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE TABLE [Inventories] (
        [Id] int NOT NULL IDENTITY,
        [StoreId] int NOT NULL,
        [ProductId] int NOT NULL,
        [Quantity] decimal(18,3) NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        [IsDeleted] bit NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Inventories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Inventories_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Inventories_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE TABLE [StockMovements] (
        [Id] int NOT NULL IDENTITY,
        [ProductId] int NOT NULL,
        [StoreId] int NOT NULL,
        [Quantity] decimal(18,3) NOT NULL,
        [PreviousQuantity] decimal(18,3) NOT NULL,
        [NewQuantity] decimal(18,3) NOT NULL,
        [MovementType] int NOT NULL,
        [ReferenceId] int NULL,
        [ReferenceNumber] nvarchar(50) NULL,
        [Reason] nvarchar(500) NULL,
        [UserId] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        [IsDeleted] bit NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_StockMovements] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StockMovements_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockMovements_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockMovements_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE TABLE [Bills] (
        [Id] int NOT NULL IDENTITY,
        [StoreId] int NOT NULL,
        [CustomerId] int NULL,
        [SalesPersonId] int NOT NULL,
        [BillNumber] nvarchar(50) NOT NULL,
        [BillDate] datetime2 NOT NULL,
        [BillType] int NOT NULL,
        [Status] int NOT NULL,
        [Subtotal] decimal(18,2) NOT NULL,
        [ItemDiscountTotal] decimal(18,2) NOT NULL,
        [BillDiscount] decimal(18,2) NOT NULL,
        [TaxAmount] decimal(18,2) NOT NULL,
        [GrandTotal] decimal(18,2) NOT NULL,
        [PaidAmount] decimal(18,2) NOT NULL,
        [DueAmount] decimal(18,2) NOT NULL,
        [WalletRedeemed] decimal(18,2) NOT NULL,
        [Notes] nvarchar(max) NULL,
        [ExchangeOfBillId] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        [IsDeleted] bit NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Bills] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Bills_GrandTotal] CHECK ([GrandTotal] >= 0),
        CONSTRAINT [FK_Bills_Bills_ExchangeOfBillId] FOREIGN KEY ([ExchangeOfBillId]) REFERENCES [Bills] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Bills_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Bills_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Bills_Users_SalesPersonId] FOREIGN KEY ([SalesPersonId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE TABLE [CustomerLedgers] (
        [Id] int NOT NULL IDENTITY,
        [CustomerId] int NOT NULL,
        [StoreId] int NOT NULL,
        [ReferenceNumber] nvarchar(50) NULL,
        [ReferenceId] int NULL,
        [Debit] decimal(18,2) NOT NULL,
        [Credit] decimal(18,2) NOT NULL,
        [Balance] decimal(18,2) NOT NULL,
        [TransactionType] int NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [TransactionDate] datetime2 NOT NULL,
        [UserId] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        [IsDeleted] bit NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_CustomerLedgers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CustomerLedgers_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CustomerLedgers_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CustomerLedgers_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE TABLE [HeldBills] (
        [Id] int NOT NULL IDENTITY,
        [StoreId] int NOT NULL,
        [CustomerId] int NULL,
        [SalesPersonId] int NOT NULL,
        [HoldReference] nvarchar(50) NOT NULL,
        [Notes] nvarchar(max) NULL,
        [BillDiscount] decimal(18,2) NOT NULL,
        [ItemsJson] nvarchar(max) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        [IsDeleted] bit NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_HeldBills] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_HeldBills_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_HeldBills_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_HeldBills_Users_SalesPersonId] FOREIGN KEY ([SalesPersonId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE TABLE [WalletTransactions] (
        [Id] int NOT NULL IDENTITY,
        [CustomerId] int NOT NULL,
        [StoreId] int NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [BalanceAfter] decimal(18,2) NOT NULL,
        [TransactionType] int NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [ReferenceId] int NULL,
        [ReferenceNumber] nvarchar(max) NULL,
        [UserId] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        [IsDeleted] bit NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_WalletTransactions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WalletTransactions_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WalletTransactions_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WalletTransactions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE TABLE [PurchaseItems] (
        [Id] int NOT NULL IDENTITY,
        [PurchaseId] int NOT NULL,
        [ProductId] int NOT NULL,
        [Quantity] decimal(18,3) NOT NULL,
        [PurchasePrice] decimal(18,2) NOT NULL,
        [Total] decimal(18,2) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        [IsDeleted] bit NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_PurchaseItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PurchaseItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseItems_Purchases_PurchaseId] FOREIGN KEY ([PurchaseId]) REFERENCES [Purchases] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE TABLE [StockTransferItems] (
        [Id] int NOT NULL IDENTITY,
        [StockTransferId] int NOT NULL,
        [ProductId] int NOT NULL,
        [Quantity] decimal(18,3) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        [IsDeleted] bit NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_StockTransferItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StockTransferItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockTransferItems_StockTransfers_StockTransferId] FOREIGN KEY ([StockTransferId]) REFERENCES [StockTransfers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE TABLE [BillItems] (
        [Id] int NOT NULL IDENTITY,
        [BillId] int NOT NULL,
        [ProductId] int NOT NULL,
        [ProductCode] nvarchar(50) NOT NULL,
        [ProductName] nvarchar(200) NOT NULL,
        [Quantity] decimal(18,3) NOT NULL,
        [Rate] decimal(18,2) NOT NULL,
        [PurchasePrice] decimal(18,2) NOT NULL,
        [DiscountAmount] decimal(18,2) NOT NULL,
        [TaxPercent] decimal(5,2) NOT NULL,
        [TaxAmount] decimal(18,2) NOT NULL,
        [Total] decimal(18,2) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        [IsDeleted] bit NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_BillItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BillItems_Bills_BillId] FOREIGN KEY ([BillId]) REFERENCES [Bills] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BillItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE TABLE [Payments] (
        [Id] int NOT NULL IDENTITY,
        [StoreId] int NOT NULL,
        [BillId] int NULL,
        [CustomerId] int NULL,
        [PaymentMode] int NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [ReferenceNumber] nvarchar(100) NULL,
        [PaymentDate] datetime2 NOT NULL,
        [Notes] nvarchar(max) NULL,
        [UserId] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        [IsDeleted] bit NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Payments_Amount] CHECK ([Amount] >= 0),
        CONSTRAINT [FK_Payments_Bills_BillId] FOREIGN KEY ([BillId]) REFERENCES [Bills] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Payments_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Payments_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Payments_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE TABLE [Referrals] (
        [Id] int NOT NULL IDENTITY,
        [StoreId] int NOT NULL,
        [ReferrerCustomerId] int NOT NULL,
        [ReferredCustomerId] int NOT NULL,
        [BillId] int NULL,
        [RewardAmount] decimal(18,2) NOT NULL,
        [Status] int NOT NULL,
        [ReferralDate] datetime2 NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        [IsDeleted] bit NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Referrals] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Referrals_Bills_BillId] FOREIGN KEY ([BillId]) REFERENCES [Bills] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Referrals_Customers_ReferredCustomerId] FOREIGN KEY ([ReferredCustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Referrals_Customers_ReferrerCustomerId] FOREIGN KEY ([ReferrerCustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Referrals_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE TABLE [Returns] (
        [Id] int NOT NULL IDENTITY,
        [StoreId] int NOT NULL,
        [OriginalBillId] int NOT NULL,
        [OriginalBillNumber] nvarchar(50) NOT NULL,
        [ReturnNumber] nvarchar(50) NOT NULL,
        [ReturnDate] datetime2 NOT NULL,
        [CustomerId] int NULL,
        [ReturnAmount] decimal(18,2) NOT NULL,
        [Reason] nvarchar(max) NULL,
        [ReturnKind] int NOT NULL,
        [UserId] int NOT NULL,
        [ExchangeBillId] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        [IsDeleted] bit NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Returns] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Returns_Bills_ExchangeBillId] FOREIGN KEY ([ExchangeBillId]) REFERENCES [Bills] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Returns_Bills_OriginalBillId] FOREIGN KEY ([OriginalBillId]) REFERENCES [Bills] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Returns_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Returns_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Returns_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE TABLE [CustomerPayments] (
        [Id] int NOT NULL IDENTITY,
        [CustomerId] int NOT NULL,
        [StoreId] int NOT NULL,
        [PaymentId] int NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        [IsDeleted] bit NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_CustomerPayments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CustomerPayments_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CustomerPayments_Payments_PaymentId] FOREIGN KEY ([PaymentId]) REFERENCES [Payments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CustomerPayments_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE TABLE [ReferralRewards] (
        [Id] int NOT NULL IDENTITY,
        [ReferralId] int NOT NULL,
        [CustomerId] int NOT NULL,
        [BillId] int NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Status] int NOT NULL,
        [IsReferrerReward] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        [IsDeleted] bit NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_ReferralRewards] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ReferralRewards_Bills_BillId] FOREIGN KEY ([BillId]) REFERENCES [Bills] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ReferralRewards_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ReferralRewards_Referrals_ReferralId] FOREIGN KEY ([ReferralId]) REFERENCES [Referrals] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE TABLE [ReturnItems] (
        [Id] int NOT NULL IDENTITY,
        [ProductReturnId] int NOT NULL,
        [OriginalBillItemId] int NOT NULL,
        [ProductId] int NOT NULL,
        [ProductCode] nvarchar(50) NOT NULL,
        [ProductName] nvarchar(200) NOT NULL,
        [Quantity] decimal(18,3) NOT NULL,
        [Rate] decimal(18,2) NOT NULL,
        [TaxAmount] decimal(18,2) NOT NULL,
        [Total] decimal(18,2) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        [IsDeleted] bit NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_ReturnItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ReturnItems_BillItems_OriginalBillItemId] FOREIGN KEY ([OriginalBillItemId]) REFERENCES [BillItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ReturnItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ReturnItems_Returns_ProductReturnId] FOREIGN KEY ([ProductReturnId]) REFERENCES [Returns] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_CreatedDate] ON [AuditLogs] ([CreatedDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_StoreId] ON [AuditLogs] ([StoreId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_UserId] ON [AuditLogs] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_BillItems_BillId] ON [BillItems] ([BillId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_BillItems_ProductId] ON [BillItems] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Bills_BillDate] ON [Bills] ([BillDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Bills_BillNumber] ON [Bills] ([BillNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Bills_CustomerId] ON [Bills] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Bills_ExchangeOfBillId] ON [Bills] ([ExchangeOfBillId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Bills_SalesPersonId] ON [Bills] ([SalesPersonId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Bills_StoreId] ON [Bills] ([StoreId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BillSequences_StoreId_FinancialYearCode] ON [BillSequences] ([StoreId], [FinancialYearCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Categories_Name] ON [Categories] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CustomerLedgers_CustomerId] ON [CustomerLedgers] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CustomerLedgers_StoreId] ON [CustomerLedgers] ([StoreId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CustomerLedgers_UserId] ON [CustomerLedgers] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CustomerPayments_CustomerId] ON [CustomerPayments] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CustomerPayments_PaymentId] ON [CustomerPayments] ([PaymentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CustomerPayments_StoreId] ON [CustomerPayments] ([StoreId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Customers_MobileNumber] ON [Customers] ([MobileNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Customers_ReferralCode] ON [Customers] ([ReferralCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Customers_ReferredByCustomerId] ON [Customers] ([ReferredByCustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Customers_StoreId] ON [Customers] ([StoreId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_HeldBills_CustomerId] ON [HeldBills] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_HeldBills_SalesPersonId] ON [HeldBills] ([SalesPersonId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_HeldBills_StoreId] ON [HeldBills] ([StoreId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Inventories_ProductId] ON [Inventories] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Inventories_StoreId_ProductId] ON [Inventories] ([StoreId], [ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PasswordResetTokens_UserId] ON [PasswordResetTokens] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Payments_BillId] ON [Payments] ([BillId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Payments_CustomerId] ON [Payments] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Payments_StoreId] ON [Payments] ([StoreId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Payments_UserId] ON [Payments] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProductImportBatches_BatchId] ON [ProductImportBatches] ([BatchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Products_Barcode] ON [Products] ([Barcode]) WHERE Barcode IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Products_CategoryId] ON [Products] ([CategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Products_ProductCode] ON [Products] ([ProductCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Products_ProductName] ON [Products] ([ProductName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PurchaseItems_ProductId] ON [PurchaseItems] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PurchaseItems_PurchaseId] ON [PurchaseItems] ([PurchaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Purchases_StoreId] ON [Purchases] ([StoreId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Purchases_UserId] ON [Purchases] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ReferralRewards_BillId] ON [ReferralRewards] ([BillId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ReferralRewards_CustomerId] ON [ReferralRewards] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ReferralRewards_ReferralId] ON [ReferralRewards] ([ReferralId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Referrals_BillId] ON [Referrals] ([BillId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Referrals_ReferredCustomerId] ON [Referrals] ([ReferredCustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Referrals_ReferrerCustomerId] ON [Referrals] ([ReferrerCustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Referrals_StoreId] ON [Referrals] ([StoreId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ReturnItems_OriginalBillItemId] ON [ReturnItems] ([OriginalBillItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ReturnItems_ProductId] ON [ReturnItems] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ReturnItems_ProductReturnId] ON [ReturnItems] ([ProductReturnId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Returns_CustomerId] ON [Returns] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Returns_ExchangeBillId] ON [Returns] ([ExchangeBillId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Returns_OriginalBillId] ON [Returns] ([OriginalBillId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Returns_ReturnNumber] ON [Returns] ([ReturnNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Returns_StoreId] ON [Returns] ([StoreId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Returns_UserId] ON [Returns] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ReturnSequences_StoreId_FinancialYearCode] ON [ReturnSequences] ([StoreId], [FinancialYearCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RevokedTokens_Jti] ON [RevokedTokens] ([Jti]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Roles_Name] ON [Roles] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_StockMovements_CreatedDate] ON [StockMovements] ([CreatedDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_StockMovements_ProductId] ON [StockMovements] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_StockMovements_StoreId] ON [StockMovements] ([StoreId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_StockMovements_UserId] ON [StockMovements] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_StockTransferItems_ProductId] ON [StockTransferItems] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_StockTransferItems_StockTransferId] ON [StockTransferItems] ([StockTransferId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_StockTransfers_FromStoreId] ON [StockTransfers] ([FromStoreId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_StockTransfers_ToStoreId] ON [StockTransfers] ([ToStoreId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StockTransfers_TransferNumber] ON [StockTransfers] ([TransferNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_StockTransfers_UserId] ON [StockTransfers] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Stores_StoreCode] ON [Stores] ([StoreCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_StoreUsers_UserId] ON [StoreUsers] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_UserName] ON [Users] ([UserName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_WalletTransactions_CustomerId] ON [WalletTransactions] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_WalletTransactions_StoreId] ON [WalletTransactions] ([StoreId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_WalletTransactions_UserId] ON [WalletTransactions] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824173438_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260824173438_InitialCreate', N'9.0.8');
END;

COMMIT;
GO


