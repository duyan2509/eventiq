using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eventiq.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class Chart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChartKey",
                schema: "identity",
                table: "EventItem");

            migrationBuilder.AddColumn<Guid>(
                name: "ChartId",
                schema: "identity",
                table: "EventItem",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Charts",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Charts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Charts_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "identity",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventItem_ChartId",
                schema: "identity",
                table: "EventItem",
                column: "ChartId");

            migrationBuilder.CreateIndex(
                name: "IX_Charts_EventId",
                schema: "identity",
                table: "Charts",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Charts_Name_EventId",
                schema: "identity",
                table: "Charts",
                columns: new[] { "Name", "EventId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_EventItem_Charts_ChartId",
                schema: "identity",
                table: "EventItem",
                column: "ChartId",
                principalSchema: "identity",
                principalTable: "Charts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventItem_Charts_ChartId",
                schema: "identity",
                table: "EventItem");

            migrationBuilder.DropTable(
                name: "Charts",
                schema: "identity");

            migrationBuilder.DropIndex(
                name: "IX_EventItem_ChartId",
                schema: "identity",
                table: "EventItem");

            migrationBuilder.DropColumn(
                name: "ChartId",
                schema: "identity",
                table: "EventItem");

            migrationBuilder.AddColumn<string>(
                name: "ChartKey",
                schema: "identity",
                table: "EventItem",
                type: "text",
                nullable: true);
        }
    }
}
