using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DomusMercatorisDotnetMVC.Migrations
{
    /// <inheritdoc />
    public partial class AddGeminiApiKeyToCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GeminiApiKey",
                table: "Companies",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GeminiApiKey",
                table: "Companies");
        }
    }
}
