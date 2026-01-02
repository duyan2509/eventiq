using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eventiq.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddVerifyRequestNAddTicketCodeTicketStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                schema: "identity",
                table: "Tickets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TicketCode",
                schema: "identity",
                table: "Tickets",
                type: "text",
                nullable: true);

            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    ticket_record RECORD;
                    new_code TEXT;
                    code_exists BOOLEAN;
                BEGIN
                    FOR ticket_record IN SELECT ""Id"" FROM identity.""Tickets"" WHERE ""TicketCode"" IS NULL
                    LOOP
                        LOOP
                            new_code := UPPER(SUBSTRING(REPLACE(gen_random_uuid()::text, '-', ''), 1, 8));
                            SELECT EXISTS(SELECT 1 FROM identity.""Tickets"" WHERE ""TicketCode"" = new_code) INTO code_exists;
                            EXIT WHEN NOT code_exists;
                        END LOOP;
                        
                        UPDATE identity.""Tickets""
                        SET ""TicketCode"" = new_code
                        WHERE ""Id"" = ticket_record.""Id"";
                    END LOOP;
                END $$;
            ");

            migrationBuilder.AlterColumn<string>(
                name: "TicketCode",
                schema: "identity",
                table: "Tickets",
                type: "text",
                nullable: false);

            migrationBuilder.CreateTable(
                name: "VerifyRequests",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    OtpHash = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VerifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerifyRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VerifyRequests_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalSchema: "identity",
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_TicketCode",
                schema: "identity",
                table: "Tickets",
                column: "TicketCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VerifyRequests_TicketId",
                schema: "identity",
                table: "VerifyRequests",
                column: "TicketId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VerifyRequests",
                schema: "identity");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_TicketCode",
                schema: "identity",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "identity",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "TicketCode",
                schema: "identity",
                table: "Tickets");
        }
    }
}
