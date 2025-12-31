using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eventiq.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckoutNRemoveSeatKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EventSeats_ChartId_SeatKey",
                schema: "identity",
                table: "EventSeats");

            migrationBuilder.DropColumn(
                name: "SeatKey",
                schema: "identity",
                table: "EventSeats");

            migrationBuilder.AlterColumn<string>(
                name: "Label",
                schema: "identity",
                table: "EventSeats",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "Checkouts",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    HoldToken = table.Column<string>(type: "text", nullable: true),
                    HoldTokenExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EventKey = table.Column<string>(type: "text", nullable: true),
                    SeatIdsJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Checkouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Checkouts_EventItem_EventItemId",
                        column: x => x.EventItemId,
                        principalSchema: "identity",
                        principalTable: "EventItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventSeats_ChartId_Label",
                schema: "identity",
                table: "EventSeats",
                columns: new[] { "ChartId", "Label" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Checkouts_EventItemId",
                schema: "identity",
                table: "Checkouts",
                column: "EventItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Checkouts",
                schema: "identity");

            migrationBuilder.DropIndex(
                name: "IX_EventSeats_ChartId_Label",
                schema: "identity",
                table: "EventSeats");

            migrationBuilder.AlterColumn<string>(
                name: "Label",
                schema: "identity",
                table: "EventSeats",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "SeatKey",
                schema: "identity",
                table: "EventSeats",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_EventSeats_ChartId_SeatKey",
                schema: "identity",
                table: "EventSeats",
                columns: new[] { "ChartId", "SeatKey" },
                unique: true);
        }
    }
}
