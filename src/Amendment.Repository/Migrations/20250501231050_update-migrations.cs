using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amendment.Repository.Migrations
{
    /// <inheritdoc />
    public partial class updatemigrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "RolesId", "UserId" },
                values: new object[] { 5, 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumns: new[] { "RolesId", "UserId" },
                keyValues: new object[] { 5, 1 });
        }
    }
}
