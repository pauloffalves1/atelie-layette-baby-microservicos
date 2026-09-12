using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtelieBebe.Orders.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderItemModerationFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ModerationFlag",
                table: "OrderItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModerationFlag",
                table: "OrderItems");
        }
    }
}
