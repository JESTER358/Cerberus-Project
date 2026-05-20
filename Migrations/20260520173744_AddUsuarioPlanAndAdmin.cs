using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoInnovador.Migrations
{
    /// <inheritdoc />
    public partial class AddUsuarioPlanAndAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Usuarios",
                type: "TEXT",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "EsAdmin",
                table: "Usuarios",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Plan",
                table: "Usuarios",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "Free");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "EsAdmin",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Plan",
                table: "Usuarios");
        }
    }
}
