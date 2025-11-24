using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniversityFinder.Migrations
{
    /// <inheritdoc />
    public partial class AddRankingAndTuitionFeeAndCityQuality : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Ranking",
                table: "Universities",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TuitionFee",
                table: "Universities",
                type: "REAL",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CityQualities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CityId = table.Column<int>(type: "INTEGER", nullable: false),
                    SafetyScore = table.Column<decimal>(type: "REAL", nullable: true),
                    HousingCost = table.Column<decimal>(type: "REAL", nullable: true),
                    EducationScore = table.Column<decimal>(type: "REAL", nullable: true),
                    HealthcareScore = table.Column<decimal>(type: "REAL", nullable: true),
                    CostOfLivingIndex = table.Column<decimal>(type: "REAL", nullable: true),
                    QualityOfLifeScore = table.Column<decimal>(type: "REAL", nullable: true),
                    EnvironmentalScore = table.Column<decimal>(type: "REAL", nullable: true),
                    EconomyScore = table.Column<decimal>(type: "REAL", nullable: true),
                    StartupScore = table.Column<decimal>(type: "REAL", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CityQualities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CityQualities_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CityQualities_CityId",
                table: "CityQualities",
                column: "CityId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CityQualities");

            migrationBuilder.DropColumn(
                name: "Ranking",
                table: "Universities");

            migrationBuilder.DropColumn(
                name: "TuitionFee",
                table: "Universities");
        }
    }
}

