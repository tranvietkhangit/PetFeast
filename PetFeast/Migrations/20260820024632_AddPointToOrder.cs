using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetFeast.Migrations
{
    /// <inheritdoc />
    public partial class AddPointToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PointDiscount",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "UsedPoints",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PointDiscount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "UsedPoints",
                table: "Orders");
        }
    }
}
