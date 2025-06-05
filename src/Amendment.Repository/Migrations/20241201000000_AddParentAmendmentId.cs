using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amendment.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddParentAmendmentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentAmendmentId",
                table: "Amendment",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Amendment_ParentAmendmentId",
                table: "Amendment",
                column: "ParentAmendmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Amendment_Amendment_ParentAmendmentId",
                table: "Amendment",
                column: "ParentAmendmentId",
                principalTable: "Amendment",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Amendment_Amendment_ParentAmendmentId",
                table: "Amendment");

            migrationBuilder.DropIndex(
                name: "IX_Amendment_ParentAmendmentId",
                table: "Amendment");

            migrationBuilder.DropColumn(
                name: "ParentAmendmentId",
                table: "Amendment");
        }
    }
}
