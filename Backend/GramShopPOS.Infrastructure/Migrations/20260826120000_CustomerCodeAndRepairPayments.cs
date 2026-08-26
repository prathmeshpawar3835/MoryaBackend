using GramShopPOS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GramShopPOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260826120000_CustomerCodeAndRepairPayments")]
    public partial class CustomerCodeAndRepairPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerCode",
                table: "Customers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE Customers
                SET CustomerCode = 'CUS' + RIGHT(REPLICATE('0', 6) + CAST(Id AS varchar(20)), 6)
                WHERE CustomerCode IS NULL OR LTRIM(RTRIM(CustomerCode)) IN ('', 'PENDING');
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CustomerCode",
                table: "Customers",
                column: "CustomerCode",
                unique: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedAmount",
                table: "RepairJobs",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalAmount",
                table: "RepairJobs",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PaidAmount",
                table: "RepairJobs",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PaymentMode",
                table: "RepairJobs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentReference",
                table: "RepairJobs",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customers_CustomerCode",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CustomerCode",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "EstimatedAmount",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "FinalAmount",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "PaidAmount",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "PaymentMode",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "PaymentReference",
                table: "RepairJobs");
        }
    }
}
