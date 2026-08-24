-- Indexes are created with tables by EF Core migrations.
-- This script is safe to re-run and documents the production index set.

USE [GramShopPOS];
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_ProductName' AND object_id = OBJECT_ID('Products'))
    CREATE INDEX IX_Products_ProductName ON Products(ProductName);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_CategoryId' AND object_id = OBJECT_ID('Products'))
    CREATE INDEX IX_Products_CategoryId ON Products(CategoryId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StockMovements_ProductId' AND object_id = OBJECT_ID('StockMovements'))
    CREATE INDEX IX_StockMovements_ProductId ON StockMovements(ProductId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StockMovements_StoreId' AND object_id = OBJECT_ID('StockMovements'))
    CREATE INDEX IX_StockMovements_StoreId ON StockMovements(StoreId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StockMovements_CreatedDate' AND object_id = OBJECT_ID('StockMovements'))
    CREATE INDEX IX_StockMovements_CreatedDate ON StockMovements(CreatedDate);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Bills_StoreId' AND object_id = OBJECT_ID('Bills'))
    CREATE INDEX IX_Bills_StoreId ON Bills(StoreId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Bills_BillDate' AND object_id = OBJECT_ID('Bills'))
    CREATE INDEX IX_Bills_BillDate ON Bills(BillDate);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Bills_CustomerId' AND object_id = OBJECT_ID('Bills'))
    CREATE INDEX IX_Bills_CustomerId ON Bills(CustomerId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CustomerLedgers_CustomerId' AND object_id = OBJECT_ID('CustomerLedgers'))
    CREATE INDEX IX_CustomerLedgers_CustomerId ON CustomerLedgers(CustomerId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CustomerLedgers_StoreId' AND object_id = OBJECT_ID('CustomerLedgers'))
    CREATE INDEX IX_CustomerLedgers_StoreId ON CustomerLedgers(StoreId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Referrals_ReferrerCustomerId' AND object_id = OBJECT_ID('Referrals'))
    CREATE INDEX IX_Referrals_ReferrerCustomerId ON Referrals(ReferrerCustomerId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Referrals_ReferredCustomerId' AND object_id = OBJECT_ID('Referrals'))
    CREATE INDEX IX_Referrals_ReferredCustomerId ON Referrals(ReferredCustomerId);
GO
