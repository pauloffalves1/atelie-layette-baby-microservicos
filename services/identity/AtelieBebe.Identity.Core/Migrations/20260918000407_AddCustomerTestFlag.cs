using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtelieBebe.Identity.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerTestFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTest",
                table: "Customers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTest",
                table: "Customers");
        }
    }
}
