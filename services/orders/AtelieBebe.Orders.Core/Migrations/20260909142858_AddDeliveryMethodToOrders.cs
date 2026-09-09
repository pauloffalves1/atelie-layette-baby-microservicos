using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtelieBebe.Orders.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryMethodToOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeliveryMethod",
                table: "Orders",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "Entrega");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryMethod",
                table: "Orders");
        }
    }
}
