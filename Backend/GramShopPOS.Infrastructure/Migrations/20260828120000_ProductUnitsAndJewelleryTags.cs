using GramShopPOS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GramShopPOS.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260828120000_ProductUnitsAndJewelleryTags")]
    public partial class ProductUnitsAndJewelleryTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodePrefix",
                table: "Categories",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "Products",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metal",
                table: "Products",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeightGrams",
                table: "Products",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductUnitSequences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Prefix = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    LastNumber = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductUnitSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductUnits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    UniqueNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    BillItemId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductUnits_BillItems_BillItemId",
                        column: x => x.BillItemId,
                        principalTable: "BillItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductUnits_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductUnits_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_CodePrefix",
                table: "Categories",
                column: "CodePrefix",
                unique: true,
                filter: "[CodePrefix] IS NOT NULL AND [CodePrefix] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_ProductUnitSequences_Prefix",
                table: "ProductUnitSequences",
                column: "Prefix",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductUnits_BillItemId",
                table: "ProductUnits",
                column: "BillItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductUnits_ProductId_StoreId_Status",
                table: "ProductUnits",
                columns: new[] { "ProductId", "StoreId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductUnits_StoreId",
                table: "ProductUnits",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductUnits_UniqueNumber",
                table: "ProductUnits",
                column: "UniqueNumber",
                unique: true);

            migrationBuilder.Sql("""
                UPDATE Categories SET CodePrefix = CASE
                    WHEN Name LIKE '%Mangalsutra%' OR Name LIKE '%Mangal%' THEN 'MGS'
                    WHEN Name LIKE '%Nose%' THEN 'NSP'
                    WHEN Name LIKE '%Bracelet%' OR Name LIKE '%Bang%' THEN 'BRC'
                    WHEN Name LIKE '%Neck%' THEN 'NCK'
                    WHEN Name LIKE '%Earring%' THEN 'ERG'
                    WHEN Name LIKE '%Chain%' THEN 'CHN'
                    WHEN Name LIKE '%Ring%' THEN 'RNG'
                    ELSE LEFT(UPPER(REPLACE(Name, ' ', '')), 3)
                END
                WHERE CodePrefix IS NULL OR CodePrefix = ''
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ProductUnits");
            migrationBuilder.DropTable(name: "ProductUnitSequences");
            migrationBuilder.DropIndex(name: "IX_Categories_CodePrefix", table: "Categories");
            migrationBuilder.DropColumn(name: "CodePrefix", table: "Categories");
            migrationBuilder.DropColumn(name: "ImagePath", table: "Products");
            migrationBuilder.DropColumn(name: "Metal", table: "Products");
            migrationBuilder.DropColumn(name: "WeightGrams", table: "Products");
        }
    }
}
