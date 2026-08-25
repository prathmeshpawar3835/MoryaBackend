using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GramShopPOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PosFeatureEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SalesPersonId",
                table: "Returns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountGiven",
                table: "Referrals",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NewCustomerPercent",
                table: "Referrals",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ReferralCode",
                table: "Referrals",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "ReferrerPercent",
                table: "Referrals",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SaleAmount",
                table: "Referrals",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "SalesPersonId",
                table: "Referrals",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ReferralRewards",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsReversal",
                table: "ReferralRewards",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LedgerEntryId",
                table: "ReferralRewards",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReturnId",
                table: "ReferralRewards",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SupplierId",
                table: "Purchases",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ReferralDiscount",
                table: "Bills",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "StoreDiscountAmount",
                table: "Bills",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "StoreDiscountId",
                table: "Bills",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RepairJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    BillId = table.Column<int>(type: "int", nullable: true),
                    BillItemId = table.Column<int>(type: "int", nullable: true),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    JobNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MobileNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProductDetails = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    JobType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReceivedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpectedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveredDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairJobs_BillItems_BillItemId",
                        column: x => x.BillItemId,
                        principalTable: "BillItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairJobs_Bills_BillId",
                        column: x => x.BillId,
                        principalTable: "Bills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairJobs_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairJobs_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairJobs_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairJobs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoreDiscounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DiscountKind = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreDiscounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreDiscounts_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactPerson = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    GSTNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Suppliers_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairJobHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RepairJobId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairJobHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairJobHistories_RepairJobs_RepairJobId",
                        column: x => x.RepairJobId,
                        principalTable: "RepairJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairJobHistories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Returns_SalesPersonId",
                table: "Returns",
                column: "SalesPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_ReferralCode",
                table: "Referrals",
                column: "ReferralCode");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_SalesPersonId",
                table: "Referrals",
                column: "SalesPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_ReferralRewards_LedgerEntryId",
                table: "ReferralRewards",
                column: "LedgerEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_ReferralRewards_ReturnId",
                table: "ReferralRewards",
                column: "ReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_SupplierId",
                table: "Purchases",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Bills_StoreDiscountId",
                table: "Bills",
                column: "StoreDiscountId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairJobHistories_RepairJobId",
                table: "RepairJobHistories",
                column: "RepairJobId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairJobHistories_UserId",
                table: "RepairJobHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairJobs_BillId",
                table: "RepairJobs",
                column: "BillId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairJobs_BillItemId",
                table: "RepairJobs",
                column: "BillItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairJobs_CustomerId",
                table: "RepairJobs",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairJobs_InvoiceNumber",
                table: "RepairJobs",
                column: "InvoiceNumber");

            migrationBuilder.CreateIndex(
                name: "IX_RepairJobs_JobNumber",
                table: "RepairJobs",
                column: "JobNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepairJobs_MobileNumber",
                table: "RepairJobs",
                column: "MobileNumber");

            migrationBuilder.CreateIndex(
                name: "IX_RepairJobs_ProductId",
                table: "RepairJobs",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairJobs_ReceivedDate",
                table: "RepairJobs",
                column: "ReceivedDate");

            migrationBuilder.CreateIndex(
                name: "IX_RepairJobs_StoreId",
                table: "RepairJobs",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairJobs_UserId",
                table: "RepairJobs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreDiscounts_StoreId",
                table: "StoreDiscounts",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Name",
                table: "Suppliers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_StoreId",
                table: "Suppliers",
                column: "StoreId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bills_StoreDiscounts_StoreDiscountId",
                table: "Bills",
                column: "StoreDiscountId",
                principalTable: "StoreDiscounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Purchases_Suppliers_SupplierId",
                table: "Purchases",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReferralRewards_CustomerLedgers_LedgerEntryId",
                table: "ReferralRewards",
                column: "LedgerEntryId",
                principalTable: "CustomerLedgers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReferralRewards_Returns_ReturnId",
                table: "ReferralRewards",
                column: "ReturnId",
                principalTable: "Returns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Referrals_Users_SalesPersonId",
                table: "Referrals",
                column: "SalesPersonId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Returns_Users_SalesPersonId",
                table: "Returns",
                column: "SalesPersonId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                UPDATE BusinessSettings
                SET RewardType = 2, NewCustomerReward = 10, ReferrerReward = 5
                WHERE RewardType = 1 AND NewCustomerReward = 50 AND ReferrerReward = 100
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bills_StoreDiscounts_StoreDiscountId",
                table: "Bills");

            migrationBuilder.DropForeignKey(
                name: "FK_Purchases_Suppliers_SupplierId",
                table: "Purchases");

            migrationBuilder.DropForeignKey(
                name: "FK_ReferralRewards_CustomerLedgers_LedgerEntryId",
                table: "ReferralRewards");

            migrationBuilder.DropForeignKey(
                name: "FK_ReferralRewards_Returns_ReturnId",
                table: "ReferralRewards");

            migrationBuilder.DropForeignKey(
                name: "FK_Referrals_Users_SalesPersonId",
                table: "Referrals");

            migrationBuilder.DropForeignKey(
                name: "FK_Returns_Users_SalesPersonId",
                table: "Returns");

            migrationBuilder.DropTable(
                name: "RepairJobHistories");

            migrationBuilder.DropTable(
                name: "StoreDiscounts");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "RepairJobs");

            migrationBuilder.DropIndex(
                name: "IX_Returns_SalesPersonId",
                table: "Returns");

            migrationBuilder.DropIndex(
                name: "IX_Referrals_ReferralCode",
                table: "Referrals");

            migrationBuilder.DropIndex(
                name: "IX_Referrals_SalesPersonId",
                table: "Referrals");

            migrationBuilder.DropIndex(
                name: "IX_ReferralRewards_LedgerEntryId",
                table: "ReferralRewards");

            migrationBuilder.DropIndex(
                name: "IX_ReferralRewards_ReturnId",
                table: "ReferralRewards");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_SupplierId",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_Bills_StoreDiscountId",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "SalesPersonId",
                table: "Returns");

            migrationBuilder.DropColumn(
                name: "DiscountGiven",
                table: "Referrals");

            migrationBuilder.DropColumn(
                name: "NewCustomerPercent",
                table: "Referrals");

            migrationBuilder.DropColumn(
                name: "ReferralCode",
                table: "Referrals");

            migrationBuilder.DropColumn(
                name: "ReferrerPercent",
                table: "Referrals");

            migrationBuilder.DropColumn(
                name: "SaleAmount",
                table: "Referrals");

            migrationBuilder.DropColumn(
                name: "SalesPersonId",
                table: "Referrals");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "ReferralRewards");

            migrationBuilder.DropColumn(
                name: "IsReversal",
                table: "ReferralRewards");

            migrationBuilder.DropColumn(
                name: "LedgerEntryId",
                table: "ReferralRewards");

            migrationBuilder.DropColumn(
                name: "ReturnId",
                table: "ReferralRewards");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "ReferralDiscount",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "StoreDiscountAmount",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "StoreDiscountId",
                table: "Bills");
        }
    }
}
