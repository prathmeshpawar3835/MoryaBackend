-- Unique / check constraints already applied by EF Core. This script is idempotent documentation.

USE [GramShopPOS];
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Bills_GrandTotal')
    ALTER TABLE Bills ADD CONSTRAINT CK_Bills_GrandTotal CHECK (GrandTotal >= 0);

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Payments_Amount')
    ALTER TABLE Payments ADD CONSTRAINT CK_Payments_Amount CHECK (Amount >= 0);
GO
