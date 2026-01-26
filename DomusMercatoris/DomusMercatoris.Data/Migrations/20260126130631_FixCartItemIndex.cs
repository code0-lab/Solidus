using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DomusMercatoris.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixCartItemIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CartItem_UserId_ProductId_VariantProductId",
                table: "CartItem");

            migrationBuilder.CreateIndex(
                name: "IX_CartItem_UserId_ProductId_VariantProductId",
                table: "CartItem",
                columns: new[] { "UserId", "ProductId", "VariantProductId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CartItem_UserId_ProductId_VariantProductId",
                table: "CartItem");

            migrationBuilder.CreateIndex(
                name: "IX_CartItem_UserId_ProductId_VariantProductId",
                table: "CartItem",
                columns: new[] { "UserId", "ProductId", "VariantProductId" },
                unique: true,
                filter: "[VariantProductId] IS NOT NULL");
        }
    }
}
