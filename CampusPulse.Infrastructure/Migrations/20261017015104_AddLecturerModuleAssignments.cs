using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusPulse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLecturerModuleAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LecturerModules",
                columns: table => new
                {
                    LecturerModuleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LecturerId = table.Column<int>(type: "int", nullable: false),
                    ModuleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LecturerModules", x => x.LecturerModuleId);
                    table.ForeignKey(
                        name: "FK_LecturerModules_Lecturers_LecturerId",
                        column: x => x.LecturerId,
                        principalTable: "Lecturers",
                        principalColumn: "LecturerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LecturerModules_Modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "Modules",
                        principalColumn: "ModuleId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LecturerModules_LecturerId_ModuleId",
                table: "LecturerModules",
                columns: new[] { "LecturerId", "ModuleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LecturerModules_ModuleId",
                table: "LecturerModules",
                column: "ModuleId");

            // Preserve lecturer-module relationships from existing timetables.
            migrationBuilder.Sql(@"
                INSERT INTO [LecturerModules] ([LecturerId], [ModuleId])
                SELECT DISTINCT t.[LecturerId], t.[ModuleId]
                FROM [Timetables] AS t
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM [LecturerModules] AS lm
                    WHERE lm.[LecturerId] = t.[LecturerId]
                      AND lm.[ModuleId] = t.[ModuleId]
                );
            ");
}

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LecturerModules");
        }
    }
}
