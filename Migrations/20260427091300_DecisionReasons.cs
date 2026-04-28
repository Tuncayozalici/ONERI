using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ONERI.Migrations
{
    [Migration("20260427091300_DecisionReasons")]
    public partial class DecisionReasons : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KararGerekcesi",
                table: "Degerlendirmeler",
                type: "TEXT",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "YoneticiKararGerekcesi",
                table: "Oneriler",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "YoneticiKararTarihi",
                table: "Oneriler",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KararGerekcesi",
                table: "Degerlendirmeler");

            migrationBuilder.DropColumn(
                name: "YoneticiKararGerekcesi",
                table: "Oneriler");

            migrationBuilder.DropColumn(
                name: "YoneticiKararTarihi",
                table: "Oneriler");
        }
    }
}
