using HelpDesk_System.Db;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpDesk_System.Migrations;

[DbContext(typeof(HelpDeskDbContext))]
[Migration("20260802180000_AddTicketAssignment")]
public partial class AddTicketAssignment : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.AddColumn<int>(
			name: "AssignedAdminId",
			table: "Tickets",
			type: "integer",
			nullable: true);

		migrationBuilder.CreateIndex(
			name: "IX_Tickets_AssignedAdminId",
			table: "Tickets",
			column: "AssignedAdminId");

		migrationBuilder.AddForeignKey(
			name: "FK_Tickets_Users_AssignedAdminId",
			table: "Tickets",
			column: "AssignedAdminId",
			principalTable: "Users",
			principalColumn: "Id",
			onDelete: ReferentialAction.SetNull);
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.DropForeignKey(name: "FK_Tickets_Users_AssignedAdminId", table: "Tickets");
		migrationBuilder.DropIndex(name: "IX_Tickets_AssignedAdminId", table: "Tickets");
		migrationBuilder.DropColumn(name: "AssignedAdminId", table: "Tickets");
	}
}
