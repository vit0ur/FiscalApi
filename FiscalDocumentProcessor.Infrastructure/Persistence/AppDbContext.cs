using FiscalDocumentProcessor.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FiscalDocumentProcessor.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    public DbSet<DocumentoFiscal> Documentos => Set<DocumentoFiscal>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var d = modelBuilder.Entity<DocumentoFiscal>();
        d.ToTable("documentos_fiscais");
        d.HasKey(x => x.Id);
        d.Property(x => x.TipoDocumento).HasConversion<string>().IsRequired();
        d.Property(x => x.ChaveAcesso).HasMaxLength(60);
        d.Property(x => x.CNPJEmitente).HasMaxLength(20);
        d.Property(x => x.CNPJDestinatario).HasMaxLength(20);
        d.Property(x => x.UF).HasMaxLength(2);
        d.Property(x => x.HashXml).HasMaxLength(128).IsRequired();
        d.Property(x => x.ValorTotal).HasColumnType("numeric(18,2)");
        d.HasIndex(x => x.ChaveAcesso).IsUnique();
        d.HasIndex(x => x.HashXml).IsUnique();
        d.Property(x => x.XmlOriginalGzip);
    }
}
