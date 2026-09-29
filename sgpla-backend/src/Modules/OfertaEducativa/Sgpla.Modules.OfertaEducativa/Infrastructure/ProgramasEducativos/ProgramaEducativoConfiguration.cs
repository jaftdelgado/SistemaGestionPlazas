using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.OfertaEducativa.Domain.ProgramasEducativos;

namespace Sgpla.Modules.OfertaEducativa.Infrastructure.ProgramasEducativos;

internal sealed class ProgramaEducativoConfiguration : IEntityTypeConfiguration<ProgramaEducativo>
{
    public void Configure(EntityTypeBuilder<ProgramaEducativo> builder)
    {
        builder.ToTable("programa_educativo", "academico");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Nombre).HasMaxLength(ProgramaEducativo.LongitudMaximaNombre);

        builder.HasQueryFilter(FiltrosConsulta.BajaLogica, p => p.FechaEliminacion == null);
    }
}
