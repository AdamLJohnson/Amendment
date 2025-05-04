using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amendment.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddIsArchivedToAmendment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Amendment",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Amendment");
        }
    }
}
