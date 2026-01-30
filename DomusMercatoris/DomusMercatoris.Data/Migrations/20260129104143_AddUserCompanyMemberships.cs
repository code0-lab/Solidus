using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DomusMercatoris.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCompanyMemberships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserCompanyMemberships",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCompanyMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCompanyMemberships_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserCompanyMemberships_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserCompanyMemberships_CompanyId",
                table: "UserCompanyMemberships",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCompanyMemberships_UserId_CompanyId",
                table: "UserCompanyMemberships",
                columns: new[] { "UserId", "CompanyId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserCompanyMemberships");
        }
    }
}
