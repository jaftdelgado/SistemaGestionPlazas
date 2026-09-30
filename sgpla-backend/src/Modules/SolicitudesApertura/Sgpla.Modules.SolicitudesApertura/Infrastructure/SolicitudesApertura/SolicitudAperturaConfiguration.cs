using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sgpla.Modules.SolicitudesApertura.Domain.SolicitudesApertura;

namespace Sgpla.Modules.SolicitudesApertura.Infrastructure.SolicitudesApertura;

internal sealed class SolicitudAperturaConfiguration : IEntityTypeConfiguration<SolicitudApertura>
{
    public void Configure(EntityTypeBuilder<SolicitudApertura> builder)
    {
        builder.ToTable("solicitud_apertura", "academico");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Seccion)
            .HasMaxLength(SolicitudApertura.LongitudMaximaSeccion)
            .IsUnicode(false);
        builder.Property(s => s.Estado)
            .HasMaxLength(20)
            .IsUnicode(false)
            .HasConversion(
                estado => estado.ToString().ToUpperInvariant(),
                valor => Enum.Parse<EstadoSolicitudApertura>(valor, ignoreCase: true));
        builder.Property(s => s.MotivoCancelacion).HasMaxLength(SolicitudApertura.LongitudMaximaMotivo);
        builder.Property(s => s.Version).IsRowVersion();

        builder.HasOne(s => s.Oficio)
            .WithOne()
            .HasForeignKey<SolicitudApertura>(s => s.OficioRespaldoId);
    }
}
