using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ONERI.Migrations
{
    [Migration("20260427093000_ManagerDecisionScores")]
    public partial class ManagerDecisionScores : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "YoneticiGayretPuani",
                table: "Oneriler",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YoneticiOrijinallikPuani",
                table: "Oneriler",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YoneticiEtkiPuani",
                table: "Oneriler",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YoneticiUygulanabilirlikPuani",
                table: "Oneriler",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YoneticiToplamPuan",
                table: "Oneriler",
                type: "INTEGER",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "YoneticiGayretPuani",
                table: "Oneriler");

            migrationBuilder.DropColumn(
                name: "YoneticiOrijinallikPuani",
                table: "Oneriler");

            migrationBuilder.DropColumn(
                name: "YoneticiEtkiPuani",
                table: "Oneriler");

            migrationBuilder.DropColumn(
                name: "YoneticiUygulanabilirlikPuani",
                table: "Oneriler");

            migrationBuilder.DropColumn(
                name: "YoneticiToplamPuan",
                table: "Oneriler");
        }
    }
}
