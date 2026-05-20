using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoInnovador.Migrations
{
    /// <inheritdoc />
    public partial class AddUsuarioIdToArchivoOriginal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UsuarioId",
                table: "ArchivosOriginales",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArchivosOriginales_UsuarioId",
                table: "ArchivosOriginales",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_ArchivosOriginales_Usuarios_UsuarioId",
                table: "ArchivosOriginales",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ArchivosOriginales_Usuarios_UsuarioId",
                table: "ArchivosOriginales");

            migrationBuilder.DropIndex(
                name: "IX_ArchivosOriginales_UsuarioId",
                table: "ArchivosOriginales");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "ArchivosOriginales");
        }
    }
}
