using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eventiq.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTaskTimeNTimeFromEventTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndTime",
                schema: "identity",
                table: "EventTasks");

            migrationBuilder.DropColumn(
                name: "StartTime",
                schema: "identity",
                table: "EventTasks");

            migrationBuilder.DropColumn(
                name: "TaskType",
                schema: "identity",
                table: "EventTasks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EndTime",
                schema: "identity",
                table: "EventTasks",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "StartTime",
                schema: "identity",
                table: "EventTasks",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "TaskType",
                schema: "identity",
                table: "EventTasks",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
