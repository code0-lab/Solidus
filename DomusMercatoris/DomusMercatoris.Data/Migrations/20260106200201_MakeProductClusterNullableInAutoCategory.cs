using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DomusMercatoris.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeProductClusterNullableInAutoCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AutoCategories_ProductClusters_ProductClusterId",
                table: "AutoCategories");

            migrationBuilder.AlterColumn<int>(
                name: "ProductClusterId",
                table: "AutoCategories",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_AutoCategories_ProductClusters_ProductClusterId",
                table: "AutoCategories",
                column: "ProductClusterId",
                principalTable: "ProductClusters",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AutoCategories_ProductClusters_ProductClusterId",
                table: "AutoCategories");

            migrationBuilder.AlterColumn<int>(
                name: "ProductClusterId",
                table: "AutoCategories",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AutoCategories_ProductClusters_ProductClusterId",
                table: "AutoCategories",
                column: "ProductClusterId",
                principalTable: "ProductClusters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
