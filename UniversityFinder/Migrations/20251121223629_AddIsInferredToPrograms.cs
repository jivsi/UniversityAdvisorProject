using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniversityFinder.Migrations
{
    /// <inheritdoc />
    public partial class AddIsInferredToPrograms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsInferred",
                table: "Programs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Programs_IsInferred",
                table: "Programs",
                column: "IsInferred");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Programs_IsInferred",
                table: "Programs");

            migrationBuilder.DropColumn(
                name: "IsInferred",
                table: "Programs");
        }
    }
}
