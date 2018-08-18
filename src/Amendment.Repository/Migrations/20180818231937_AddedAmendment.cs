using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Amendment.Repository.Migrations
{
    public partial class AddedAmendment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Language",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    LanguageName = table.Column<string>(nullable: false),
                    LanguageCode = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Language", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Amendment",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AmendTitle = table.Column<string>(nullable: false),
                    Author = table.Column<string>(nullable: true),
                    Motion = table.Column<string>(nullable: true),
                    Source = table.Column<string>(nullable: true),
                    LegisId = table.Column<string>(nullable: true),
                    PrimaryLanguageId = table.Column<int>(nullable: false),
                    EnteredBy = table.Column<int>(nullable: false),
                    EnteredDate = table.Column<DateTime>(nullable: false),
                    LastUpdatedBy = table.Column<int>(nullable: false),
                    LastUpdated = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Amendment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Amendment_Language_PrimaryLanguageId",
                        column: x => x.PrimaryLanguageId,
                        principalTable: "Language",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AmendmentBody",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AmendId = table.Column<int>(nullable: false),
                    LanguageId = table.Column<int>(nullable: false),
                    AmendTitle = table.Column<string>(nullable: true),
                    AmendBody = table.Column<string>(nullable: false),
                    AmendStatus = table.Column<int>(nullable: false),
                    EnteredBy = table.Column<int>(nullable: false),
                    EnteredDate = table.Column<DateTime>(nullable: false),
                    LastUpdatedBy = table.Column<int>(nullable: false),
                    LastUpdated = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AmendmentBody", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AmendmentBody_Amendment_AmendId",
                        column: x => x.AmendId,
                        principalTable: "Amendment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AmendmentBody_Language_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Language",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Amendment_PrimaryLanguageId",
                table: "Amendment",
                column: "PrimaryLanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_AmendmentBody_AmendId",
                table: "AmendmentBody",
                column: "AmendId");

            migrationBuilder.CreateIndex(
                name: "IX_AmendmentBody_LanguageId",
                table: "AmendmentBody",
                column: "LanguageId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AmendmentBody");

            migrationBuilder.DropTable(
                name: "Amendment");

            migrationBuilder.DropTable(
                name: "Language");
        }
    }
}
