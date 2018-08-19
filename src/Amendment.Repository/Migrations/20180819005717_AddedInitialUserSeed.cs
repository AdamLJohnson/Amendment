using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Amendment.Repository.Migrations
{
    public partial class AddedInitialUserSeed : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Language",
                columns: new[] { "Id", "LanguageCode", "LanguageName" },
                values: new object[,]
                {
                    { 1, "ENG", "English" },
                    { 2, "SPA", "Spanish" },
                    { 3, "FRA", "French" }
                });

            migrationBuilder.InsertData(
                table: "Role",
                columns: new[] { "Id", "EnteredBy", "EnteredDate", "LastUpdated", "LastUpdatedBy", "Name" },
                values: new object[,]
                {
                    { 1, -1, new DateTime(2018, 8, 19, 0, 57, 17, 452, DateTimeKind.Utc), new DateTime(2018, 8, 19, 0, 57, 17, 452, DateTimeKind.Utc), -1, "System Administrator" },
                    { 2, -1, new DateTime(2018, 8, 19, 0, 57, 17, 452, DateTimeKind.Utc), new DateTime(2018, 8, 19, 0, 57, 17, 452, DateTimeKind.Utc), -1, "Screen Controller" },
                    { 3, -1, new DateTime(2018, 8, 19, 0, 57, 17, 452, DateTimeKind.Utc), new DateTime(2018, 8, 19, 0, 57, 17, 452, DateTimeKind.Utc), -1, "Amendment Editor" },
                    { 4, -1, new DateTime(2018, 8, 19, 0, 57, 17, 452, DateTimeKind.Utc), new DateTime(2018, 8, 19, 0, 57, 17, 452, DateTimeKind.Utc), -1, "Translator" }
                });

            migrationBuilder.InsertData(
                table: "User",
                columns: new[] { "Id", "Email", "EnteredBy", "EnteredDate", "LastUpdated", "LastUpdatedBy", "Name", "Password", "Username" },
                values: new object[] { 1, "admin@admin.com", -1, new DateTime(2018, 8, 19, 0, 57, 17, 450, DateTimeKind.Utc), new DateTime(2018, 8, 19, 0, 57, 17, 450, DateTimeKind.Utc), -1, "Admin", "$2b$12$HbvEC6UaeXiGGlv8ztvzL.SB6oBXKi2QoBkJsjwQvDJGpQ59CmWrq", "admin" });

            migrationBuilder.InsertData(
                table: "UserXRole",
                columns: new[] { "UserId", "RoleId" },
                values: new object[] { 1, 1 });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Language",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Language",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Language",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Role",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Role",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Role",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "UserXRole",
                keyColumns: new[] { "UserId", "RoleId" },
                keyValues: new object[] { 1, 1 });

            migrationBuilder.DeleteData(
                table: "Role",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "User",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
