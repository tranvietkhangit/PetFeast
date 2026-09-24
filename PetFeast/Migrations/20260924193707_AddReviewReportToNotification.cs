using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetFeast.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewReportToNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReviewReportId",
                table: "Notifications",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ReviewReportId",
                table: "Notifications",
                column: "ReviewReportId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_ReviewReports_ReviewReportId",
                table: "Notifications",
                column: "ReviewReportId",
                principalTable: "ReviewReports",
                principalColumn: "ReviewReportId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_ReviewReports_ReviewReportId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_ReviewReportId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ReviewReportId",
                table: "Notifications");
        }
    }
}
