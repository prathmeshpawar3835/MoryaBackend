using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GramShopPOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InvoiceDiscountSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BirthdayDiscountPercent",
                table: "Bills",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ReferralDiscountPercent",
                table: "Bills",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ReferrerBenefitAmount",
                table: "Bills",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ReferrerBenefitPercent",
                table: "Bills",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ReferrerCode",
                table: "Bills",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReferrerCustomerId",
                table: "Bills",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferrerName",
                table: "Bills",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoreDiscountName",
                table: "Bills",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StoreDiscountPercent",
                table: "Bills",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_Bills_ReferrerCustomerId",
                table: "Bills",
                column: "ReferrerCustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bills_Customers_ReferrerCustomerId",
                table: "Bills",
                column: "ReferrerCustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bills_Customers_ReferrerCustomerId",
                table: "Bills");

            migrationBuilder.DropIndex(
                name: "IX_Bills_ReferrerCustomerId",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "BirthdayDiscountPercent",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "ReferralDiscountPercent",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "ReferrerBenefitAmount",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "ReferrerBenefitPercent",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "ReferrerCode",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "ReferrerCustomerId",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "ReferrerName",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "StoreDiscountName",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "StoreDiscountPercent",
                table: "Bills");
        }
    }
}
