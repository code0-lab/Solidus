using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DomusMercatoris.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentCodeToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentCode",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentCode",
                table: "Orders");
        }
    }
}
