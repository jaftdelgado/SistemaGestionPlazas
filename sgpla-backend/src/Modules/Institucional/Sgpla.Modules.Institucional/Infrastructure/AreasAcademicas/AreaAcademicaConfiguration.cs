using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Institucional.Domain.AreasAcademicas;

namespace Sgpla.Modules.Institucional.Infrastructure.AreasAcademicas;

internal sealed class AreaAcademicaConfiguration : IEntityTypeConfiguration<AreaAcademica>
{
    public void Configure(EntityTypeBuilder<AreaAcademica> builder)
    {
        builder.ToTable("area_academica", "academico");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Nombre).HasMaxLength(AreaAcademica.LongitudMaximaNombre);

        // char(10) en la base.
        builder.Property(a => a.Telefono).HasMaxLength(AreaAcademica.LongitudTelefono).IsUnicode(false).IsFixedLength();

        // varchar(10) en la base.
        builder.Property(a => a.Extension).HasMaxLength(AreaAcademica.LongitudMaximaExtension).IsUnicode(false);

        builder.HasQueryFilter(FiltrosConsulta.BajaLogica, a => a.FechaEliminacion == null);
    }
}
