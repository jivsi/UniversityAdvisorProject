using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniversityFinder.Migrations
{
    /// <inheritdoc />
    public partial class AddSubjectAliasTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubjectAliases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SubjectId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    LanguageCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubjectAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubjectAliases_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubjectAliases_LanguageCode",
                table: "SubjectAliases",
                column: "LanguageCode");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectAliases_Name",
                table: "SubjectAliases",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectAliases_SubjectId",
                table: "SubjectAliases",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectAliases_SubjectId_Name_LanguageCode",
                table: "SubjectAliases",
                columns: new[] { "SubjectId", "Name", "LanguageCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubjectAliases");
        }
    }
}
