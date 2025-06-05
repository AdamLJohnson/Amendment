using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amendment.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddIsApprovedToAmendment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "Amendment",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "Amendment");
        }
    }
}
