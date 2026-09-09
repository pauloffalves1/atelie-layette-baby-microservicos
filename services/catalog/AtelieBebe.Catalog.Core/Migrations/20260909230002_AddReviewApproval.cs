using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtelieBebe.Catalog.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Approved",
                table: "ProductReviews",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviews_Approved",
                table: "ProductReviews",
                column: "Approved");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductReviews_Approved",
                table: "ProductReviews");

            migrationBuilder.DropColumn(
                name: "Approved",
                table: "ProductReviews");
        }
    }
}
