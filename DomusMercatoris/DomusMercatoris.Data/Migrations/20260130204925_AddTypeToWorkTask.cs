using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DomusMercatoris.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTypeToWorkTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "WorkTasks",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "WorkTasks");
        }
    }
}
