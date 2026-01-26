using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ONERI.Migrations
{
    /// <inheritdoc />
    public partial class Faz2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BolumYoneticileri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BolumAdi = table.Column<string>(type: "TEXT", nullable: false),
                    YoneticiAdi = table.Column<string>(type: "TEXT", nullable: false),
                    YoneticiEmail = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BolumYoneticileri", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Degerlendirmeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OneriId = table.Column<int>(type: "INTEGER", nullable: false),
                    GayretPuanu = table.Column<int>(type: "INTEGER", nullable: false),
                    OrijinallikPuanu = table.Column<int>(type: "INTEGER", nullable: false),
                    DirektEtkiPuanu = table.Column<int>(type: "INTEGER", nullable: false),
                    DolayliEtkiPuanu = table.Column<int>(type: "INTEGER", nullable: false),
                    ToplamPuan = table.Column<int>(type: "INTEGER", nullable: false),
                    KurulYorumu = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Degerlendirmeler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Degerlendirmeler_Oneriler_OneriId",
                        column: x => x.OneriId,
                        principalTable: "Oneriler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Degerlendirmeler_OneriId",
                table: "Degerlendirmeler",
                column: "OneriId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BolumYoneticileri");

            migrationBuilder.DropTable(
                name: "Degerlendirmeler");
        }
    }
}
