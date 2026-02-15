using System;
using FiscalDocumentProcessor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FiscalDocumentProcessor.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
public partial class AppDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.6")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        modelBuilder.Entity("FiscalDocumentProcessor.Domain.Entities.DocumentoFiscal", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd();

            b.Property<string>("CNPJDestinatario")
                .HasMaxLength(20);

            b.Property<string>("CNPJEmitente")
                .HasMaxLength(20);

            b.Property<string>("ChaveAcesso")
                .HasMaxLength(60);

            b.Property<DateTime?>("DataEmissao");

            b.Property<DateTime>("DataProcessamento");

            b.Property<string>("HashXml")
                .IsRequired()
                .HasMaxLength(128);

            b.Property<int>("Status");

            b.Property<string>("TipoDocumento")
                .IsRequired();

            b.Property<string>("UF")
                .HasMaxLength(2);

            b.Property<decimal?>("ValorTotal")
                .HasColumnType("numeric(18,2)");

            b.Property<byte[]>("XmlOriginalGzip");

            b.HasKey("Id");

            b.HasIndex("ChaveAcesso")
                .IsUnique();

            b.HasIndex("HashXml")
                .IsUnique();

            b.ToTable("documentos_fiscais");
        });
    }
}
