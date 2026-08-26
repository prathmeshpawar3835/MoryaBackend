using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GramShopPOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: these columns exist on the model but were missing from earlier migrations.
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Returns', 'DeductionAmount') IS NULL
    ALTER TABLE [Returns] ADD [DeductionAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_Returns_DeductionAmount] DEFAULT 0;
IF COL_LENGTH('dbo.Returns', 'DeductionPercent') IS NULL
    ALTER TABLE [Returns] ADD [DeductionPercent] decimal(5,2) NOT NULL CONSTRAINT [DF_Returns_DeductionPercent] DEFAULT 0;
IF COL_LENGTH('dbo.Returns', 'GrossAmount') IS NULL
    ALTER TABLE [Returns] ADD [GrossAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_Returns_GrossAmount] DEFAULT 0;
IF COL_LENGTH('dbo.BusinessSettings', 'BuybackDeductionPercent') IS NULL
    ALTER TABLE [BusinessSettings] ADD [BuybackDeductionPercent] decimal(5,2) NOT NULL CONSTRAINT [DF_BusinessSettings_BuybackDeductionPercent] DEFAULT 0;
IF COL_LENGTH('dbo.BusinessSettings', 'ExchangeDeductionPercent') IS NULL
    ALTER TABLE [BusinessSettings] ADD [ExchangeDeductionPercent] decimal(5,2) NOT NULL CONSTRAINT [DF_BusinessSettings_ExchangeDeductionPercent] DEFAULT 0;
IF COL_LENGTH('dbo.BusinessSettings', 'ReturnDeductionPercent') IS NULL
    ALTER TABLE [BusinessSettings] ADD [ReturnDeductionPercent] decimal(5,2) NOT NULL CONSTRAINT [DF_BusinessSettings_ReturnDeductionPercent] DEFAULT 0;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Returns', 'DeductionAmount') IS NOT NULL
    ALTER TABLE [Returns] DROP COLUMN [DeductionAmount];
IF COL_LENGTH('dbo.Returns', 'DeductionPercent') IS NOT NULL
    ALTER TABLE [Returns] DROP COLUMN [DeductionPercent];
IF COL_LENGTH('dbo.Returns', 'GrossAmount') IS NOT NULL
    ALTER TABLE [Returns] DROP COLUMN [GrossAmount];
IF COL_LENGTH('dbo.BusinessSettings', 'BuybackDeductionPercent') IS NOT NULL
    ALTER TABLE [BusinessSettings] DROP COLUMN [BuybackDeductionPercent];
IF COL_LENGTH('dbo.BusinessSettings', 'ExchangeDeductionPercent') IS NOT NULL
    ALTER TABLE [BusinessSettings] DROP COLUMN [ExchangeDeductionPercent];
IF COL_LENGTH('dbo.BusinessSettings', 'ReturnDeductionPercent') IS NOT NULL
    ALTER TABLE [BusinessSettings] DROP COLUMN [ReturnDeductionPercent];
");
        }
    }
}
