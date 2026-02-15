using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscalDocumentProcessor.Infrastructure.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "documentos_fiscais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoDocumento = table.Column<string>(type: "text", nullable: false),
                    ChaveAcesso = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    CNPJEmitente = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CNPJDestinatario = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    UF = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    DataEmissao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValorTotal = table.Column<decimal>(type: "numeric", nullable: true),
                    XmlOriginalGzip = table.Column<byte[]>(type: "bytea", nullable: true),
                    HashXml = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DataProcessamento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documentos_fiscais", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_documentos_fiscais_ChaveAcesso",
                table: "documentos_fiscais",
                column: "ChaveAcesso",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_documentos_fiscais_HashXml",
                table: "documentos_fiscais",
                column: "HashXml",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "documentos_fiscais");
        }
    }
}
