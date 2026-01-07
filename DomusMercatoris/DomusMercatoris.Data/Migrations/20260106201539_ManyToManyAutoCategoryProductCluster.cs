using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DomusMercatoris.Data.Migrations
{
    /// <inheritdoc />
    public partial class ManyToManyAutoCategoryProductCluster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AutoCategories_ProductClusters_ProductClusterId",
                table: "AutoCategories");

            migrationBuilder.DropIndex(
                name: "IX_AutoCategories_ProductClusterId",
                table: "AutoCategories");

            migrationBuilder.DropColumn(
                name: "ProductClusterId",
                table: "AutoCategories");

            migrationBuilder.CreateTable(
                name: "AutoCategoryProductCluster",
                columns: table => new
                {
                    AutoCategoriesId = table.Column<int>(type: "int", nullable: false),
                    ProductClustersId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoCategoryProductCluster", x => new { x.AutoCategoriesId, x.ProductClustersId });
                    table.ForeignKey(
                        name: "FK_AutoCategoryProductCluster_AutoCategories_AutoCategoriesId",
                        column: x => x.AutoCategoriesId,
                        principalTable: "AutoCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AutoCategoryProductCluster_ProductClusters_ProductClustersId",
                        column: x => x.ProductClustersId,
                        principalTable: "ProductClusters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AutoCategoryProductCluster_ProductClustersId",
                table: "AutoCategoryProductCluster",
                column: "ProductClustersId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AutoCategoryProductCluster");

            migrationBuilder.AddColumn<int>(
                name: "ProductClusterId",
                table: "AutoCategories",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AutoCategories_ProductClusterId",
                table: "AutoCategories",
                column: "ProductClusterId");

            migrationBuilder.AddForeignKey(
                name: "FK_AutoCategories_ProductClusters_ProductClusterId",
                table: "AutoCategories",
                column: "ProductClusterId",
                principalTable: "ProductClusters",
                principalColumn: "Id");
        }
    }
}
