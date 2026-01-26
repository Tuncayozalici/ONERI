using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ONERI.Migrations
{
    /// <inheritdoc />
    public partial class Faz2_Puanlama_Eklendi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DirektEtkiPuanu",
                table: "Degerlendirmeler");

            migrationBuilder.RenameColumn(
                name: "OrijinallikPuanu",
                table: "Degerlendirmeler",
                newName: "OrijinallikPuani");

            migrationBuilder.RenameColumn(
                name: "GayretPuanu",
                table: "Degerlendirmeler",
                newName: "GayretPuani");

            migrationBuilder.RenameColumn(
                name: "DolayliEtkiPuanu",
                table: "Degerlendirmeler",
                newName: "EtkiPuani");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OrijinallikPuani",
                table: "Degerlendirmeler",
                newName: "OrijinallikPuanu");

            migrationBuilder.RenameColumn(
                name: "GayretPuani",
                table: "Degerlendirmeler",
                newName: "GayretPuanu");

            migrationBuilder.RenameColumn(
                name: "EtkiPuani",
                table: "Degerlendirmeler",
                newName: "DolayliEtkiPuanu");

            migrationBuilder.AddColumn<int>(
                name: "DirektEtkiPuanu",
                table: "Degerlendirmeler",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
