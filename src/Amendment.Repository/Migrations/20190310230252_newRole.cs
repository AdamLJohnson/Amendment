using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Amendment.Repository.Migrations
{
    public partial class newRole : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Role",
                columns: new[] { "Id", "EnteredBy", "EnteredDate", "LastUpdated", "LastUpdatedBy", "Name" },
                values: new object[] { 5, -1, new DateTime(2018, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2018, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), -1, "Toast Notifications" });

            migrationBuilder.UpdateData(
                table: "SystemSetting",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EnteredDate", "LastUpdated" },
                values: new object[] { new DateTime(2019, 3, 10, 23, 2, 52, 171, DateTimeKind.Utc).AddTicks(5750), new DateTime(2019, 3, 10, 23, 2, 52, 171, DateTimeKind.Utc).AddTicks(6542) });

            migrationBuilder.UpdateData(
                table: "SystemSetting",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "EnteredDate", "LastUpdated" },
                values: new object[] { new DateTime(2019, 3, 10, 23, 2, 52, 171, DateTimeKind.Utc).AddTicks(7270), new DateTime(2019, 3, 10, 23, 2, 52, 171, DateTimeKind.Utc).AddTicks(7275) });

            migrationBuilder.InsertData(
                table: "UserXRole",
                columns: new[] { "UserId", "RoleId" },
                values: new object[] { 1, 5 });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "UserXRole",
                keyColumns: new[] { "UserId", "RoleId" },
                keyValues: new object[] { 1, 5 });

            migrationBuilder.DeleteData(
                table: "Role",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.UpdateData(
                table: "SystemSetting",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EnteredDate", "LastUpdated" },
                values: new object[] { new DateTime(2018, 12, 16, 23, 23, 35, 675, DateTimeKind.Utc), new DateTime(2018, 12, 16, 23, 23, 35, 676, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "SystemSetting",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "EnteredDate", "LastUpdated" },
                values: new object[] { new DateTime(2018, 12, 16, 23, 23, 35, 676, DateTimeKind.Utc), new DateTime(2018, 12, 16, 23, 23, 35, 676, DateTimeKind.Utc) });
        }
    }
}
