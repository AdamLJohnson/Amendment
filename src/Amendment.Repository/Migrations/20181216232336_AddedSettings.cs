using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Amendment.Repository.Migrations
{
    public partial class AddedSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemSetting",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Key = table.Column<string>(nullable: false),
                    Value = table.Column<string>(nullable: false),
                    EnteredBy = table.Column<int>(nullable: false),
                    EnteredDate = table.Column<DateTime>(nullable: false),
                    LastUpdatedBy = table.Column<int>(nullable: false),
                    LastUpdated = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSetting", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "SystemSetting",
                columns: new[] { "Id", "EnteredBy", "EnteredDate", "Key", "LastUpdated", "LastUpdatedBy", "Value" },
                values: new object[] { 1, -1, new DateTime(2018, 12, 16, 23, 23, 35, 675, DateTimeKind.Utc), "ShowDeafSigner", new DateTime(2018, 12, 16, 23, 23, 35, 676, DateTimeKind.Utc), -1, "1" });

            migrationBuilder.InsertData(
                table: "SystemSetting",
                columns: new[] { "Id", "EnteredBy", "EnteredDate", "Key", "LastUpdated", "LastUpdatedBy", "Value" },
                values: new object[] { 2, -1, new DateTime(2018, 12, 16, 23, 23, 35, 676, DateTimeKind.Utc), "ShowDeafSignerBox", new DateTime(2018, 12, 16, 23, 23, 35, 676, DateTimeKind.Utc), -1, "1" });

            migrationBuilder.CreateIndex(
                name: "IX_SystemSetting_Key",
                table: "SystemSetting",
                column: "Key",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemSetting");
        }
    }
}
