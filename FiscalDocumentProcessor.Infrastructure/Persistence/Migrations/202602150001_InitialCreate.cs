using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FiscalDocumentProcessor.Infrastructure.Persistence.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "documentos_fiscais",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                TipoDocumento = table.Column<string>(nullable: false),
                ChaveAcesso = table.Column<string>(maxLength: 60, nullable: true),
                CNPJEmitente = table.Column<string>(maxLength: 20, nullable: true),
                CNPJDestinatario = table.Column<string>(maxLength: 20, nullable: true),
                UF = table.Column<string>(maxLength: 2, nullable: true),
                DataEmissao = table.Column<DateTime>(nullable: true),
                ValorTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                XmlOriginalGzip = table.Column<byte[]>(nullable: true),
                HashXml = table.Column<string>(maxLength: 128, nullable: false),
                DataProcessamento = table.Column<DateTime>(nullable: false),
                Status = table.Column<int>(nullable: false)
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
