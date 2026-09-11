using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtelieBebe.Catalog.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionLeadTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductionLeadTimeDays",
                table: "Products",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductionLeadTimeDays",
                table: "Products");
        }
    }
}
