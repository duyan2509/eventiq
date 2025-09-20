using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eventiq.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class EventUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Events_Start_End_Status",
                schema: "identity",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "End",
                schema: "identity",
                table: "Events");

            migrationBuilder.CreateIndex(
                name: "IX_Events_Start_Status",
                schema: "identity",
                table: "Events",
                columns: new[] { "Start", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Events_Start_Status",
                schema: "identity",
                table: "Events");

            migrationBuilder.AddColumn<DateTime>(
                name: "End",
                schema: "identity",
                table: "Events",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Events_Start_End_Status",
                schema: "identity",
                table: "Events",
                columns: new[] { "Start", "End", "Status" });
        }
    }
}
