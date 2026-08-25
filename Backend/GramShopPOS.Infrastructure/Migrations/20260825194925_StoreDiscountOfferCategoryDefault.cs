using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GramShopPOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StoreDiscountOfferCategoryDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE StoreDiscounts SET OfferCategory = 1 WHERE OfferCategory = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
