using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpDesk_System.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeBasePublishing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsKnowledgeBasePublished",
                table: "Tickets",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsKnowledgeBasePublished",
                table: "Tickets");
        }
    }
}
