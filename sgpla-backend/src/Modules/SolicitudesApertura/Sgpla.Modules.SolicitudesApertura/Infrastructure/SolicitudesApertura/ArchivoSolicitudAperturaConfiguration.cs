using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sgpla.Modules.SolicitudesApertura.Domain.SolicitudesApertura;

namespace Sgpla.Modules.SolicitudesApertura.Infrastructure.SolicitudesApertura;

internal sealed class ArchivoSolicitudAperturaConfiguration : IEntityTypeConfiguration<ArchivoSolicitudApertura>
{
    public void Configure(EntityTypeBuilder<ArchivoSolicitudApertura> builder)
    {
        builder.ToTable("archivo_solicitud_apertura", "academico");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Nombre).HasMaxLength(ArchivoSolicitudApertura.LongitudMaximaNombre);
        builder.Property(a => a.Mime)
            .HasMaxLength(ArchivoSolicitudApertura.LongitudMaximaMime)
            .IsUnicode(false);
        builder.Property(a => a.ChecksumSha256)
            .HasMaxLength(ArchivoSolicitudApertura.LongitudChecksum)
            .IsFixedLength();
        builder.Property(a => a.ClaveAlmacenamiento).HasMaxLength(ArchivoSolicitudApertura.LongitudMaximaClave);
    }
}
