using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtelieBebe.Orders.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddPixQrCodeTextToOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PixQrCodeText",
                table: "Orders",
                type: "TEXT",
                maxLength: 600,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PixQrCodeText",
                table: "Orders");
        }
    }
}
