using Microsoft.EntityFrameworkCore.Migrations;

namespace Amendment.Repository.Migrations
{
    public partial class AddedPageColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Page",
                table: "AmendmentBody",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Page",
                table: "AmendmentBody");
        }
    }
}
