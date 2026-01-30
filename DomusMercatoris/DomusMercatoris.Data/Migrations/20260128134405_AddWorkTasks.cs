using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DomusMercatoris.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkTasks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrderId = table.Column<long>(type: "bigint", nullable: true),
                    AssignedToUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    ParentId = table.Column<long>(type: "bigint", nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkTasks_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WorkTasks_Users_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkTasks_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkTasks_WorkTasks_ParentId",
                        column: x => x.ParentId,
                        principalTable: "WorkTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_AssignedToUserId",
                table: "WorkTasks",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_CreatedByUserId",
                table: "WorkTasks",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_OrderId",
                table: "WorkTasks",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_ParentId",
                table: "WorkTasks",
                column: "ParentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkTasks");
        }
    }
}
