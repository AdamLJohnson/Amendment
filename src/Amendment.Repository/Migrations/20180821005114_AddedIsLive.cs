using Microsoft.EntityFrameworkCore.Migrations;

namespace Amendment.Repository.Migrations
{
    public partial class AddedIsLive : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLive",
                table: "AmendmentBody",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsLive",
                table: "Amendment",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLive",
                table: "AmendmentBody");

            migrationBuilder.DropColumn(
                name: "IsLive",
                table: "Amendment");
        }
    }
}
