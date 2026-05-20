using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoInnovador.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicIdToArchivoOriginal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "ArchivosOriginales",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Asignar GUIDs únicos a filas existentes antes de crear el índice UNIQUE.
            // SQLite no tiene UUID() nativo — usamos hex(randomblob(16)) y lo formateamos
            // como GUID estándar (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx).
            migrationBuilder.Sql(@"
                UPDATE ArchivosOriginales
                SET PublicId = lower(
                    substr(hex(randomblob(4)),1,8) || '-' ||
                    substr(hex(randomblob(2)),1,4) || '-' ||
                    '4' || substr(hex(randomblob(2)),2,3) || '-' ||
                    substr('89ab', abs(random()) % 4 + 1, 1) || substr(hex(randomblob(2)),2,3) || '-' ||
                    substr(hex(randomblob(6)),1,12)
                )
                WHERE PublicId = '00000000-0000-0000-0000-000000000000';
            ");

            migrationBuilder.CreateIndex(
                name: "IX_ArchivosOriginales_PublicId",
                table: "ArchivosOriginales",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ArchivosOriginales_PublicId",
                table: "ArchivosOriginales");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "ArchivosOriginales");
        }
    }
}
