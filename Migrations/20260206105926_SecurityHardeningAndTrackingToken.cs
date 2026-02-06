using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ONERI.Migrations
{
    /// <inheritdoc />
    public partial class SecurityHardeningAndTrackingToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Degerlendirmeler_OneriId",
                table: "Degerlendirmeler");

            migrationBuilder.AddColumn<Guid>(
                name: "TrackingToken",
                table: "Oneriler",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "YoneticiEmail",
                table: "BolumYoneticileri",
                type: "TEXT",
                maxLength: 256,
                nullable: false,
                collation: "NOCASE",
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "BolumAdi",
                table: "BolumYoneticileri",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                collation: "NOCASE",
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.Sql(
                """
                UPDATE Oneriler
                SET TrackingToken = lower(
                    hex(randomblob(4)) || '-' ||
                    hex(randomblob(2)) || '-4' ||
                    substr(hex(randomblob(2)), 2) || '-' ||
                    substr('89ab', abs(random()) % 4 + 1, 1) ||
                    substr(hex(randomblob(2)), 2) || '-' ||
                    hex(randomblob(6))
                )
                WHERE lower(TrackingToken) = '00000000-0000-0000-0000-000000000000';
                """);

            migrationBuilder.Sql(
                """
                UPDATE BolumYoneticileri
                SET
                    BolumAdi = trim(BolumAdi),
                    YoneticiAdi = trim(YoneticiAdi),
                    YoneticiEmail = trim(YoneticiEmail);

                DELETE FROM BolumYoneticileri
                WHERE length(BolumAdi) = 0
                   OR length(YoneticiAdi) = 0
                   OR length(YoneticiEmail) = 0;

                DELETE FROM BolumYoneticileri
                WHERE Id NOT IN (
                    SELECT MIN(Id)
                    FROM BolumYoneticileri
                    GROUP BY lower(BolumAdi)
                );
                """);

            migrationBuilder.Sql(
                """
                UPDATE Degerlendirmeler
                SET
                    GayretPuani = CASE
                        WHEN GayretPuani < 0 THEN 0
                        WHEN GayretPuani > 25 THEN 25
                        ELSE GayretPuani
                    END,
                    OrijinallikPuani = CASE
                        WHEN OrijinallikPuani < 0 THEN 0
                        WHEN OrijinallikPuani > 25 THEN 25
                        ELSE OrijinallikPuani
                    END,
                    EtkiPuani = CASE
                        WHEN EtkiPuani < 0 THEN 0
                        WHEN EtkiPuani > 25 THEN 25
                        ELSE EtkiPuani
                    END,
                    UygulanabilirlikPuani = CASE
                        WHEN UygulanabilirlikPuani < 0 THEN 0
                        WHEN UygulanabilirlikPuani > 25 THEN 25
                        ELSE UygulanabilirlikPuani
                    END;

                UPDATE Degerlendirmeler
                SET ToplamPuan = GayretPuani + OrijinallikPuani + EtkiPuani + UygulanabilirlikPuani;

                DELETE FROM Degerlendirmeler
                WHERE Id NOT IN (
                    SELECT MIN(Id)
                    FROM Degerlendirmeler
                    GROUP BY OneriId
                );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Oneriler_TrackingToken",
                table: "Oneriler",
                column: "TrackingToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Degerlendirmeler_OneriId",
                table: "Degerlendirmeler",
                column: "OneriId",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Degerlendirme_EtkiPuani_Range",
                table: "Degerlendirmeler",
                sql: "[EtkiPuani] BETWEEN 0 AND 25");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Degerlendirme_GayretPuani_Range",
                table: "Degerlendirmeler",
                sql: "[GayretPuani] BETWEEN 0 AND 25");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Degerlendirme_OrijinallikPuani_Range",
                table: "Degerlendirmeler",
                sql: "[OrijinallikPuani] BETWEEN 0 AND 25");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Degerlendirme_ToplamPuan_Consistency",
                table: "Degerlendirmeler",
                sql: "[ToplamPuan] = [GayretPuani] + [OrijinallikPuani] + [EtkiPuani] + [UygulanabilirlikPuani]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Degerlendirme_ToplamPuan_Range",
                table: "Degerlendirmeler",
                sql: "[ToplamPuan] BETWEEN 0 AND 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Degerlendirme_UygulanabilirlikPuani_Range",
                table: "Degerlendirmeler",
                sql: "[UygulanabilirlikPuani] BETWEEN 0 AND 25");

            migrationBuilder.CreateIndex(
                name: "IX_BolumYoneticileri_BolumAdi",
                table: "BolumYoneticileri",
                column: "BolumAdi",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_BolumYonetici_BolumAdi_NotEmpty",
                table: "BolumYoneticileri",
                sql: "length(trim([BolumAdi])) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BolumYonetici_YoneticiAdi_NotEmpty",
                table: "BolumYoneticileri",
                sql: "length(trim([YoneticiAdi])) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BolumYonetici_YoneticiEmail_NotEmpty",
                table: "BolumYoneticileri",
                sql: "length(trim([YoneticiEmail])) > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Oneriler_TrackingToken",
                table: "Oneriler");

            migrationBuilder.DropIndex(
                name: "IX_Degerlendirmeler_OneriId",
                table: "Degerlendirmeler");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Degerlendirme_EtkiPuani_Range",
                table: "Degerlendirmeler");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Degerlendirme_GayretPuani_Range",
                table: "Degerlendirmeler");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Degerlendirme_OrijinallikPuani_Range",
                table: "Degerlendirmeler");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Degerlendirme_ToplamPuan_Consistency",
                table: "Degerlendirmeler");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Degerlendirme_ToplamPuan_Range",
                table: "Degerlendirmeler");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Degerlendirme_UygulanabilirlikPuani_Range",
                table: "Degerlendirmeler");

            migrationBuilder.DropIndex(
                name: "IX_BolumYoneticileri_BolumAdi",
                table: "BolumYoneticileri");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BolumYonetici_BolumAdi_NotEmpty",
                table: "BolumYoneticileri");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BolumYonetici_YoneticiAdi_NotEmpty",
                table: "BolumYoneticileri");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BolumYonetici_YoneticiEmail_NotEmpty",
                table: "BolumYoneticileri");

            migrationBuilder.DropColumn(
                name: "TrackingToken",
                table: "Oneriler");

            migrationBuilder.AlterColumn<string>(
                name: "YoneticiEmail",
                table: "BolumYoneticileri",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 256,
                oldCollation: "NOCASE");

            migrationBuilder.AlterColumn<string>(
                name: "BolumAdi",
                table: "BolumYoneticileri",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 100,
                oldCollation: "NOCASE");

            migrationBuilder.CreateIndex(
                name: "IX_Degerlendirmeler_OneriId",
                table: "Degerlendirmeler",
                column: "OneriId");
        }
    }
}
