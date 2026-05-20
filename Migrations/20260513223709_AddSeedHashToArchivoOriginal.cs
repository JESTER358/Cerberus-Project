using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoInnovador.Migrations
{
    /// <inheritdoc />
    public partial class AddSeedHashToArchivoOriginal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SeedHash",
                table: "ArchivosOriginales",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SeedHash",
                table: "ArchivosOriginales");
        }
    }
}
