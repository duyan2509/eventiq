using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eventiq.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class EventIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Events_Name",
                schema: "identity",
                table: "Events",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_Start_End_Status",
                schema: "identity",
                table: "Events",
                columns: new[] { "Start", "End", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Events_Name",
                schema: "identity",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_Start_End_Status",
                schema: "identity",
                table: "Events");
        }
    }
}
