using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DomusMercatoris.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAutoCategoryToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AutoCategoryId",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_AutoCategoryId",
                table: "Products",
                column: "AutoCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_AutoCategories_AutoCategoryId",
                table: "Products",
                column: "AutoCategoryId",
                principalTable: "AutoCategories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_AutoCategories_AutoCategoryId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_AutoCategoryId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "AutoCategoryId",
                table: "Products");
        }
    }
}
