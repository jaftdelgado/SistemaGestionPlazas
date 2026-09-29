using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.OfertaEducativa.Domain.Programaciones;

namespace Sgpla.Modules.OfertaEducativa.Infrastructure.Programaciones;

internal sealed class ProgramacionAcademicaConfiguration : IEntityTypeConfiguration<ProgramacionAcademica>
{
    public void Configure(EntityTypeBuilder<ProgramacionAcademica> builder)
    {
        builder.ToTable("programacion_academica", "academico");
        builder.HasKey(p => p.Id);

        // varchar en la base.
        builder.Property(p => p.Nrc).HasMaxLength(ProgramacionAcademica.LongitudMaximaNrc).IsUnicode(false);

        builder.HasQueryFilter(FiltrosConsulta.BajaLogica, p => p.FechaEliminacion == null);
    }
}
