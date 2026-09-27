using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Institucional.Domain.EntidadesAcademicas;

namespace Sgpla.Modules.Institucional.Infrastructure.EntidadesAcademicas;

internal sealed class EntidadAcademicaConfiguration : IEntityTypeConfiguration<EntidadAcademica>
{
    public void Configure(EntityTypeBuilder<EntidadAcademica> builder)
    {
        builder.ToTable("entidad_academica", "academico");
        builder.HasKey(e => e.Id);

        // varchar en la base.
        builder.Property(e => e.Clave).HasMaxLength(EntidadAcademica.LongitudMaximaClave).IsUnicode(false);

        builder.Property(e => e.Nombre).HasMaxLength(EntidadAcademica.LongitudMaximaNombre);
        builder.Property(e => e.Calle).HasMaxLength(EntidadAcademica.LongitudMaximaCalle);
        builder.Property(e => e.NumeroExterior).HasMaxLength(EntidadAcademica.LongitudMaximaNumeroExterior);
        builder.Property(e => e.Colonia).HasMaxLength(EntidadAcademica.LongitudMaximaColonia);

        // char(5) y char(10) en la base.
        builder.Property(e => e.CodigoPostal).HasMaxLength(EntidadAcademica.LongitudCodigoPostal).IsUnicode(false).IsFixedLength();
        builder.Property(e => e.Telefono).HasMaxLength(EntidadAcademica.LongitudTelefono).IsUnicode(false).IsFixedLength();

        // varchar(10) en la base.
        builder.Property(e => e.Extension).HasMaxLength(EntidadAcademica.LongitudMaximaExtension).IsUnicode(false);

        builder.HasQueryFilter(FiltrosConsulta.BajaLogica, e => e.FechaEliminacion == null);
    }
}
