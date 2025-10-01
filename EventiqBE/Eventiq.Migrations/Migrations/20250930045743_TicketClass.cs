using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eventiq.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class TicketClass : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TicketClasses_Name",
                schema: "identity",
                table: "TicketClasses");

            migrationBuilder.CreateIndex(
                name: "IX_TicketClasses_Name_EventId",
                schema: "identity",
                table: "TicketClasses",
                columns: new[] { "Name", "EventId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TicketClasses_Name_EventId",
                schema: "identity",
                table: "TicketClasses");

            migrationBuilder.CreateIndex(
                name: "IX_TicketClasses_Name",
                schema: "identity",
                table: "TicketClasses",
                column: "Name",
                unique: true);
        }
    }
}
