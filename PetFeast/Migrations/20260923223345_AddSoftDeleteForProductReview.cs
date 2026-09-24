using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetFeast.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteForProductReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ProductReviews",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ProductReviews");
        }
    }
}
