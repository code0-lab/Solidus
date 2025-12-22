using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DomusMercatorisDotnetMVC.Migrations
{
    /// <inheritdoc />
    public partial class AddAiModerationToCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAiModerationEnabled",
                table: "Companies",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAiModerationEnabled",
                table: "Companies");
        }
    }
}
