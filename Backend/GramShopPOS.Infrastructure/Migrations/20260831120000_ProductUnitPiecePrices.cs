using GramShopPOS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GramShopPOS.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260831120000_ProductUnitPiecePrices")]
    public partial class ProductUnitPiecePrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PurchasePrice",
                table: "ProductUnits",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SellingPrice",
                table: "ProductUnits",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MRP",
                table: "ProductUnits",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE u
                SET u.PurchasePrice = p.PurchasePrice,
                    u.SellingPrice = p.SellingPrice,
                    u.MRP = p.MRP
                FROM [ProductUnits] u
                INNER JOIN [Products] p ON p.Id = u.ProductId
                WHERE u.PurchasePrice IS NULL OR u.SellingPrice IS NULL OR u.MRP IS NULL
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "PurchasePrice", table: "ProductUnits");
            migrationBuilder.DropColumn(name: "SellingPrice", table: "ProductUnits");
            migrationBuilder.DropColumn(name: "MRP", table: "ProductUnits");
        }
    }
}
