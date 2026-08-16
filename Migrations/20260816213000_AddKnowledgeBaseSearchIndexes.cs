using HelpDesk_System.Db;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpDesk_System.Migrations;

[DbContext(typeof(HelpDeskDbContext))]
[Migration("20260816213000_AddKnowledgeBaseSearchIndexes")]
public partial class AddKnowledgeBaseSearchIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE INDEX "IX_Tickets_FullTextSearch"
            ON "Tickets"
            USING GIN (
                to_tsvector(
                    'simple',
                    coalesce("Title", '') || ' ' || coalesce("Description", '')
                )
            );
            """);

        migrationBuilder.Sql(
            """
            CREATE INDEX "IX_TicketResponses_FullTextSearch"
            ON "TicketResponses"
            USING GIN (
                to_tsvector('simple', coalesce("Message", ''))
            );
            """);

        migrationBuilder.Sql(
            """
            CREATE INDEX "IX_RegistrationRequests_FullTextSearch"
            ON "RegistrationRequests"
            USING GIN (
                to_tsvector(
                    'simple',
                    coalesce("FirstName", '') || ' ' ||
                    coalesce("LastName", '') || ' ' ||
                    coalesce("Email", '') || ' ' ||
                    coalesce("DecisionReason", '')
                )
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP INDEX "IX_RegistrationRequests_FullTextSearch";
            """);

        migrationBuilder.Sql(
            """
            DROP INDEX "IX_TicketResponses_FullTextSearch";
            """);

        migrationBuilder.Sql(
            """
            DROP INDEX "IX_Tickets_FullTextSearch";
            """);
    }
}
