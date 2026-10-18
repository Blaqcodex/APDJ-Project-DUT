using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusPulse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentModuleUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StudentModules_StudentId",
                table: "StudentModules");

            migrationBuilder.CreateIndex(
                name: "IX_StudentModules_StudentId_ModuleId",
                table: "StudentModules",
                columns: new[] { "StudentId", "ModuleId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StudentModules_StudentId_ModuleId",
                table: "StudentModules");

            migrationBuilder.CreateIndex(
                name: "IX_StudentModules_StudentId",
                table: "StudentModules",
                column: "StudentId");
        }
    }
}
