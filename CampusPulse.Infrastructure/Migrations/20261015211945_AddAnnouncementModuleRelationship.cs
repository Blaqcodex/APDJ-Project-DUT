using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusPulse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnnouncementModuleRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ModuleId",
                table: "Announcements",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_ModuleId",
                table: "Announcements",
                column: "ModuleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Announcements_Modules_ModuleId",
                table: "Announcements",
                column: "ModuleId",
                principalTable: "Modules",
                principalColumn: "ModuleId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Announcements_Modules_ModuleId",
                table: "Announcements");

            migrationBuilder.DropIndex(
                name: "IX_Announcements_ModuleId",
                table: "Announcements");

            migrationBuilder.DropColumn(
                name: "ModuleId",
                table: "Announcements");
        }
    }
}
