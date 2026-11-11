using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arquivos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ArquivoPublico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Publico",
                schema: "arquivos",
                table: "arquivos",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Publico",
                schema: "arquivos",
                table: "arquivos");
        }
    }
}
