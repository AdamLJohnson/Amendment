using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Amendment.Repository.Migrations
{
    public partial class UpdatesToModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmendTitle",
                table: "AmendmentBody");

            migrationBuilder.UpdateData(
                table: "Role",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EnteredDate", "LastUpdated" },
                values: new object[] { new DateTime(2018, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2018, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.UpdateData(
                table: "Role",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "EnteredDate", "LastUpdated" },
                values: new object[] { new DateTime(2018, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2018, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.UpdateData(
                table: "Role",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "EnteredDate", "LastUpdated" },
                values: new object[] { new DateTime(2018, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2018, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.UpdateData(
                table: "Role",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "EnteredDate", "LastUpdated" },
                values: new object[] { new DateTime(2018, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2018, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EnteredDate", "LastUpdated" },
                values: new object[] { new DateTime(2018, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2018, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AmendTitle",
                table: "AmendmentBody",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Role",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EnteredDate", "LastUpdated" },
                values: new object[] { new DateTime(2018, 8, 19, 0, 57, 17, 452, DateTimeKind.Utc), new DateTime(2018, 8, 19, 0, 57, 17, 452, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "Role",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "EnteredDate", "LastUpdated" },
                values: new object[] { new DateTime(2018, 8, 19, 0, 57, 17, 452, DateTimeKind.Utc), new DateTime(2018, 8, 19, 0, 57, 17, 452, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "Role",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "EnteredDate", "LastUpdated" },
                values: new object[] { new DateTime(2018, 8, 19, 0, 57, 17, 452, DateTimeKind.Utc), new DateTime(2018, 8, 19, 0, 57, 17, 452, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "Role",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "EnteredDate", "LastUpdated" },
                values: new object[] { new DateTime(2018, 8, 19, 0, 57, 17, 452, DateTimeKind.Utc), new DateTime(2018, 8, 19, 0, 57, 17, 452, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EnteredDate", "LastUpdated" },
                values: new object[] { new DateTime(2018, 8, 19, 0, 57, 17, 450, DateTimeKind.Utc), new DateTime(2018, 8, 19, 0, 57, 17, 450, DateTimeKind.Utc) });
        }
    }
}
