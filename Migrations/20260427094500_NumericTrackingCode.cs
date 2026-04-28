using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ONERI.Migrations
{
    [Migration("20260427094500_NumericTrackingCode")]
    public partial class NumericTrackingCode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TakipKodu",
                table: "Oneriler",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE Oneriler
                SET TakipKodu = Id - 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Oneriler_TakipKodu",
                table: "Oneriler",
                column: "TakipKodu",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Oneriler_TakipKodu",
                table: "Oneriler");

            migrationBuilder.DropColumn(
                name: "TakipKodu",
                table: "Oneriler");
        }
    }
}
