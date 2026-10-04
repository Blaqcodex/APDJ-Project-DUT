using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusPulse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InsertDUTInstitution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Institutions",
                columns: new[] { "InstitutionId", "InstitutionName", "StudentEmailDomain" },
                values: new object[]
                {
                    1,
                    "Durban University of Technology",
                    "dut4life.ac.za"
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Institutions",
                keyColumn: "InstitutionId",
                keyValue: 1);
        }
    }
}