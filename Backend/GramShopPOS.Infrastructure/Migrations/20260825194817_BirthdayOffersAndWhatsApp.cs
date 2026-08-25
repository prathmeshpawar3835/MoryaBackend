using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GramShopPOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BirthdayOffersAndWhatsApp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "StoreDiscounts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OfferCategory",
                table: "StoreDiscounts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "WhatsAppAccessToken",
                table: "BusinessSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WhatsAppApiBaseUrl",
                table: "BusinessSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WhatsAppEnabled",
                table: "BusinessSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "WhatsAppPhoneNumberId",
                table: "BusinessSettings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BirthdayOfferId",
                table: "Bills",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BirthdayOfferName",
                table: "Bills",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BirthdayMessageLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    BirthdayOfferId = table.Column<int>(type: "int", nullable: true),
                    MobileNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BirthdayDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    OfferName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BirthdayMessageLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BirthdayMessageLogs_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BirthdayMessageLogs_StoreDiscounts_BirthdayOfferId",
                        column: x => x.BirthdayOfferId,
                        principalTable: "StoreDiscounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BirthdayMessageLogs_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BirthdayOfferRedemptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    BirthdayOfferId = table.Column<int>(type: "int", nullable: false),
                    BillId = table.Column<int>(type: "int", nullable: false),
                    SalesPersonId = table.Column<int>(type: "int", nullable: false),
                    BirthdayDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BirthdayOfferRedemptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BirthdayOfferRedemptions_Bills_BillId",
                        column: x => x.BillId,
                        principalTable: "Bills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BirthdayOfferRedemptions_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BirthdayOfferRedemptions_StoreDiscounts_BirthdayOfferId",
                        column: x => x.BirthdayOfferId,
                        principalTable: "StoreDiscounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BirthdayOfferRedemptions_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BirthdayOfferRedemptions_Users_SalesPersonId",
                        column: x => x.SalesPersonId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoreDiscounts_StoreId_OfferCategory",
                table: "StoreDiscounts",
                columns: new[] { "StoreId", "OfferCategory" });

            migrationBuilder.CreateIndex(
                name: "IX_Bills_BirthdayOfferId",
                table: "Bills",
                column: "BirthdayOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_BirthdayMessageLogs_BirthdayOfferId",
                table: "BirthdayMessageLogs",
                column: "BirthdayOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_BirthdayMessageLogs_CustomerId_BirthdayDate",
                table: "BirthdayMessageLogs",
                columns: new[] { "CustomerId", "BirthdayDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BirthdayMessageLogs_Status",
                table: "BirthdayMessageLogs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BirthdayMessageLogs_StoreId",
                table: "BirthdayMessageLogs",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_BirthdayOfferRedemptions_BillId",
                table: "BirthdayOfferRedemptions",
                column: "BillId");

            migrationBuilder.CreateIndex(
                name: "IX_BirthdayOfferRedemptions_BirthdayOfferId",
                table: "BirthdayOfferRedemptions",
                column: "BirthdayOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_BirthdayOfferRedemptions_CustomerId_BirthdayDate_Status",
                table: "BirthdayOfferRedemptions",
                columns: new[] { "CustomerId", "BirthdayDate", "Status" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BirthdayOfferRedemptions_SalesPersonId",
                table: "BirthdayOfferRedemptions",
                column: "SalesPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_BirthdayOfferRedemptions_StoreId",
                table: "BirthdayOfferRedemptions",
                column: "StoreId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bills_StoreDiscounts_BirthdayOfferId",
                table: "Bills",
                column: "BirthdayOfferId",
                principalTable: "StoreDiscounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bills_StoreDiscounts_BirthdayOfferId",
                table: "Bills");

            migrationBuilder.DropTable(
                name: "BirthdayMessageLogs");

            migrationBuilder.DropTable(
                name: "BirthdayOfferRedemptions");

            migrationBuilder.DropIndex(
                name: "IX_StoreDiscounts_StoreId_OfferCategory",
                table: "StoreDiscounts");

            migrationBuilder.DropIndex(
                name: "IX_Bills_BirthdayOfferId",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "StoreDiscounts");

            migrationBuilder.DropColumn(
                name: "OfferCategory",
                table: "StoreDiscounts");

            migrationBuilder.DropColumn(
                name: "WhatsAppAccessToken",
                table: "BusinessSettings");

            migrationBuilder.DropColumn(
                name: "WhatsAppApiBaseUrl",
                table: "BusinessSettings");

            migrationBuilder.DropColumn(
                name: "WhatsAppEnabled",
                table: "BusinessSettings");

            migrationBuilder.DropColumn(
                name: "WhatsAppPhoneNumberId",
                table: "BusinessSettings");

            migrationBuilder.DropColumn(
                name: "BirthdayOfferId",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "BirthdayOfferName",
                table: "Bills");
        }
    }
}
