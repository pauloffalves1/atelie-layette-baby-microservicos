using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtelieBebe.Orders.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddGiftMessageToOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GiftMessage",
                table: "Orders",
                type: "TEXT",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GiftMessage",
                table: "Orders");
        }
    }
}
