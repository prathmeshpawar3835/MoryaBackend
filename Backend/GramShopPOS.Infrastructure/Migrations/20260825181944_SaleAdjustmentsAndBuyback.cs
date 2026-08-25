using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GramShopPOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SaleAdjustmentsAndBuyback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AppliedToBillId",
                table: "Returns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateOfBirth",
                table: "Customers",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BirthdayDiscountPercent",
                table: "BusinessSettings",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BirthdayDiscount",
                table: "Bills",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BuybackAdjustment",
                table: "Bills",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CreditGenerated",
                table: "Bills",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeAdjustment",
                table: "Bills",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PayableAmount",
                table: "Bills",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ReturnAdjustment",
                table: "Bills",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_Returns_AppliedToBillId",
                table: "Returns",
                column: "AppliedToBillId");

            migrationBuilder.AddForeignKey(
                name: "FK_Returns_Bills_AppliedToBillId",
                table: "Returns",
                column: "AppliedToBillId",
                principalTable: "Bills",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Returns_Bills_AppliedToBillId",
                table: "Returns");

            migrationBuilder.DropIndex(
                name: "IX_Returns_AppliedToBillId",
                table: "Returns");

            migrationBuilder.DropColumn(
                name: "AppliedToBillId",
                table: "Returns");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "BirthdayDiscountPercent",
                table: "BusinessSettings");

            migrationBuilder.DropColumn(
                name: "BirthdayDiscount",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "BuybackAdjustment",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "CreditGenerated",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "ExchangeAdjustment",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "PayableAmount",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "ReturnAdjustment",
                table: "Bills");
        }
    }
}
