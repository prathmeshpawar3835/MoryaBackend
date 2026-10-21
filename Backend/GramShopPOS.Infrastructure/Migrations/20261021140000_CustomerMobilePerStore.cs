using GramShopPOS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GramShopPOS.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261021140000_CustomerMobilePerStore")]
    public partial class CustomerMobilePerStore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customers_MobileNumber",
                table: "Customers");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_StoreId_MobileNumber",
                table: "Customers",
                columns: new[] { "StoreId", "MobileNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customers_StoreId_MobileNumber",
                table: "Customers");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_MobileNumber",
                table: "Customers",
                column: "MobileNumber",
                unique: true);
        }
    }
}
