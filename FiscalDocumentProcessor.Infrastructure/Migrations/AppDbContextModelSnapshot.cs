using System;
using FiscalDocumentProcessor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace FiscalDocumentProcessor.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("ProductVersion", "8.0.6");
            modelBuilder.HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("FiscalDocumentProcessor.Domain.Entities.DocumentoFiscal", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid");

                    b.Property<string>("TipoDocumento")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ChaveAcesso")
                        .HasMaxLength(60)
                        .HasColumnType("character varying(60)");

                    b.Property<string>("CNPJEmitente")
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)");

                    b.Property<string>("CNPJDestinatario")
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)");

                    b.Property<string>("UF")
                        .HasMaxLength(2)
                        .HasColumnType("character varying(2)");

                    b.Property<DateTime?>("DataEmissao")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("DataProcessamento")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("HashXml")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)");

                    b.Property<decimal?>("ValorTotal")
                        .HasColumnType("numeric");

                    b.Property<byte[]>("XmlOriginalGzip")
                        .HasColumnType("bytea");

                    b.Property<int>("Status")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("ChaveAcesso")
                        .IsUnique();

                    b.HasIndex("HashXml")
                        .IsUnique();

                    b.ToTable("documentos_fiscais");
                });
        }
    }
}
