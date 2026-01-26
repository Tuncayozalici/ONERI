using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ONERI.Migrations
{
    /// <inheritdoc />
    public partial class Bolum_Alanlari_Eklendi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AltBolum",
                table: "Oneriler",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CalistigiBolum",
                table: "Oneriler",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AltBolum",
                table: "Oneriler");

            migrationBuilder.DropColumn(
                name: "CalistigiBolum",
                table: "Oneriler");
        }
    }
}
