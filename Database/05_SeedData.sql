USE [GramShopPOS];
GO

IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = N'Admin')
    INSERT INTO Roles (Name, Description, CreatedDate, IsDeleted, IsActive) VALUES (N'Admin', N'Full access', SYSUTCDATETIME(), 0, 1);

IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = N'SalesPerson')
    INSERT INTO Roles (Name, Description, CreatedDate, IsDeleted, IsActive) VALUES (N'SalesPerson', N'Store sales access', SYSUTCDATETIME(), 0, 1);

IF NOT EXISTS (SELECT 1 FROM Stores WHERE StoreCode = N'STORE01')
    INSERT INTO Stores (StoreCode, StoreName, Address, ContactNumber, GSTNumber, InvoicePrefix, CreatedDate, IsDeleted, IsActive)
    VALUES (N'STORE01', N'Gram Shop Main', N'MG Road, Sample City', N'9999999999', N'22AAAAA0000A1Z5', N'STORE01', SYSUTCDATETIME(), 0, 1);

IF NOT EXISTS (SELECT 1 FROM Categories WHERE Name = N'Chains')
    INSERT INTO Categories (Name, Description, CreatedDate, IsDeleted, IsActive) VALUES (N'Chains', N'Gold chains', SYSUTCDATETIME(), 0, 1);

IF NOT EXISTS (SELECT 1 FROM Categories WHERE Name = N'Rings')
    INSERT INTO Categories (Name, Description, CreatedDate, IsDeleted, IsActive) VALUES (N'Rings', N'Gold rings', SYSUTCDATETIME(), 0, 1);

IF NOT EXISTS (SELECT 1 FROM Categories WHERE Name = N'Earrings')
    INSERT INTO Categories (Name, Description, CreatedDate, IsDeleted, IsActive) VALUES (N'Earrings', N'Gold earrings', SYSUTCDATETIME(), 0, 1);

IF NOT EXISTS (SELECT 1 FROM BusinessSettings)
    INSERT INTO BusinessSettings
    (ShopName, Address, Mobile, Email, GSTNumber, InvoiceFooter, ReturnPolicy, InvoicePrefix, InvoiceNumberFormat,
     FinancialYearStartMonth, AllowNegativeStock, DefaultTaxPercent, LowStockDefaultLevel, NewCustomerReward, ReferrerReward,
     RewardType, RewardTrigger, ReferralStoreWise, ReferralEnabled, CreatedDate, IsDeleted, IsActive)
    VALUES
    (N'1 Gram Jewellery Shop', N'MG Road, Sample City', N'9999999999', N'shop@example.com', N'22AAAAA0000A1Z5',
     N'Thank you for shopping with us.', N'Returns accepted within 7 days with original invoice.', N'INV',
     N'{PREFIX}-FY{FY}-{SEQ:000000}', 4, 0, 3, 2, 50, 100, 1, 1, 0, 1, SYSUTCDATETIME(), 0, 1);

IF NOT EXISTS (SELECT 1 FROM TaxSettings WHERE Name = N'GST 3%')
    INSERT INTO TaxSettings (Name, Percent, IsDefault, CreatedDate, IsDeleted, IsActive) VALUES (N'GST 3%', 3, 1, SYSUTCDATETIME(), 0, 1);

-- Users (admin / salesperson) are created by the application seeder so passwords are hashed with ASP.NET Identity.
-- Default logins after first API start:
--   admin / ChangeMe@123
--   salesperson / ChangeMe@123
GO
