using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Amendment.Repository.Migrations
{
    public partial class ChangeDataTypeToSupportMoreChars : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AmendBody",
                table: "AmendmentBody",
                type: "longtext character set utf16",
                nullable: false,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                name: "Author",
                table: "Amendment",
                type: "longtext character set utf16",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Amendment",
                type: "longtext character set utf16",
                nullable: false,
                oldClrType: typeof(string));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AmendBody",
                table: "AmendmentBody",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext character set utf16");

            migrationBuilder.AlterColumn<string>(
                name: "Author",
                table: "Amendment",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext character set utf16",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Amendment",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext character set utf16");
        }
    }
}
