using GramShopPOS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GramShopPOS.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260912120000_EcommerceStorefront")]
    public partial class EcommerceStorefront : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EcommerceOnly",
                table: "StoreDiscounts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "EcommerceNotifyMobile",
                table: "BusinessSettings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Customers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "EcommerceOnly", table: "StoreDiscounts");
            migrationBuilder.DropColumn(name: "EcommerceNotifyMobile", table: "BusinessSettings");
            migrationBuilder.DropColumn(name: "PasswordHash", table: "Customers");
        }
    }
}
