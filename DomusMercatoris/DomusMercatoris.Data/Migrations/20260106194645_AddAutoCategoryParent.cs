using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DomusMercatoris.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAutoCategoryParent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "AutoCategories",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AutoCategories_ParentId",
                table: "AutoCategories",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_AutoCategories_AutoCategories_ParentId",
                table: "AutoCategories",
                column: "ParentId",
                principalTable: "AutoCategories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AutoCategories_AutoCategories_ParentId",
                table: "AutoCategories");

            migrationBuilder.DropIndex(
                name: "IX_AutoCategories_ParentId",
                table: "AutoCategories");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "AutoCategories");
        }
    }
}
