using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtelieBebe.Catalog.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddProductPreviousSlugs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductPreviousSlugs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductPreviousSlugs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductPreviousSlugs_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductPreviousSlugs_ProductId",
                table: "ProductPreviousSlugs",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPreviousSlugs_Slug",
                table: "ProductPreviousSlugs",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductPreviousSlugs");
        }
    }
}
