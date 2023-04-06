using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amendment.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddBodyAmendLangUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AmendmentBody_AmendId_LanguageId",
                table: "AmendmentBody");

            migrationBuilder.CreateIndex(
                name: "IX_AmendmentBody_AmendId_LanguageId",
                table: "AmendmentBody",
                columns: new[] { "AmendId", "LanguageId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AmendmentBody_AmendId_LanguageId",
                table: "AmendmentBody");

            migrationBuilder.CreateIndex(
                name: "IX_AmendmentBody_AmendId_LanguageId",
                table: "AmendmentBody",
                columns: new[] { "AmendId", "LanguageId" });
        }
    }
}
