USE [GramShopPOS];
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetNextBillNumber
    @StoreId INT,
    @FinancialYearCode NVARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE BillSequences
    SET LastNumber = LastNumber + 1,
        UpdatedDate = SYSUTCDATETIME()
    OUTPUT INSERTED.LastNumber
    WHERE StoreId = @StoreId AND FinancialYearCode = @FinancialYearCode;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetNextReturnNumber
    @StoreId INT,
    @FinancialYearCode NVARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE ReturnSequences
    SET LastNumber = LastNumber + 1,
        UpdatedDate = SYSUTCDATETIME()
    OUTPUT INSERTED.LastNumber
    WHERE StoreId = @StoreId AND FinancialYearCode = @FinancialYearCode;
END
GO
