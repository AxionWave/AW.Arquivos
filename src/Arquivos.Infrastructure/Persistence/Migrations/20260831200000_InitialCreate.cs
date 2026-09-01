using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arquivos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "arquivos");

            migrationBuilder.CreateTable(
                name: "arquivos",
                schema: "arquivos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    NomeOriginal = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    NomeArmazenado = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Extensao = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ContentType = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    TamanhoBytes = table.Column<long>(type: "bigint", nullable: false),
                    ChecksumSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CaminhoStorage = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    ProvedorStorage = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EmpresaId = table.Column<int>(type: "integer", nullable: false),
                    UsuarioUploadId = table.Column<int>(type: "integer", nullable: true),
                    SistemaOrigem = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ModuloOrigem = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ReferenciaExterna = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Metadados = table.Column<string>(type: "jsonb", nullable: false),
                    IpOrigem = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    DataCriacao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DataAtualizacao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DataDesativacao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UsuarioDesativacaoId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_arquivos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_arquivos_ChecksumSha256",
                schema: "arquivos",
                table: "arquivos",
                column: "ChecksumSha256");

            migrationBuilder.CreateIndex(
                name: "IX_arquivos_EmpresaId",
                schema: "arquivos",
                table: "arquivos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_arquivos_EmpresaId_Ativo",
                schema: "arquivos",
                table: "arquivos",
                columns: new[] { "EmpresaId", "Ativo" });

            migrationBuilder.CreateIndex(
                name: "IX_arquivos_SistemaOrigem",
                schema: "arquivos",
                table: "arquivos",
                column: "SistemaOrigem");

            migrationBuilder.CreateIndex(
                name: "IX_arquivos_Token",
                schema: "arquivos",
                table: "arquivos",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "arquivos",
                schema: "arquivos");
        }
    }
}
