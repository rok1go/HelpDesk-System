using HelpDesk_System.Db;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HelpDesk_System.Migrations;

[DbContext(typeof(HelpDeskDbContext))]
[Migration("20260818120000_AddTicketHistory")]
public partial class AddTicketHistory : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "TicketHistoryEntries",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation(
                        "Npgsql:ValueGenerationStrategy",
                        NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Description = table.Column<string>(type: "text", nullable: false),
                CreatedAt = table.Column<DateTime>(
                    type: "timestamp with time zone",
                    nullable: false),
                TicketId = table.Column<int>(type: "integer", nullable: false),
                ActorId = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TicketHistoryEntries", x => x.Id);
                table.ForeignKey(
                    name: "FK_TicketHistoryEntries_Tickets_TicketId",
                    column: x => x.TicketId,
                    principalTable: "Tickets",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_TicketHistoryEntries_Users_ActorId",
                    column: x => x.ActorId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "IX_TicketHistoryEntries_ActorId",
            table: "TicketHistoryEntries",
            column: "ActorId");

        migrationBuilder.CreateIndex(
            name: "IX_TicketHistoryEntries_TicketId",
            table: "TicketHistoryEntries",
            column: "TicketId");

        migrationBuilder.Sql(
            """
            INSERT INTO "TicketHistoryEntries" (
                "Description",
                "CreatedAt",
                "TicketId",
                "ActorId"
            )
            SELECT
                'Ticket created.',
                "CreatedAt",
                "Id",
                "AuthorId"
            FROM "Tickets";
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "TicketHistoryEntries");
    }
}
