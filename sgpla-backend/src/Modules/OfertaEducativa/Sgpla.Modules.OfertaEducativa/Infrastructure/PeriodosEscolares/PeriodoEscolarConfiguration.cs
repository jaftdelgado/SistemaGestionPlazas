using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.OfertaEducativa.Domain.PeriodosEscolares;

namespace Sgpla.Modules.OfertaEducativa.Infrastructure.PeriodosEscolares;

internal sealed class PeriodoEscolarConfiguration : IEntityTypeConfiguration<PeriodoEscolar>
{
    public void Configure(EntityTypeBuilder<PeriodoEscolar> builder)
    {
        builder.ToTable("periodo_escolar", "academico");
        builder.HasKey(p => p.Id);

        // char(6) en la base.
        builder.Property(p => p.Clave).HasMaxLength(PeriodoEscolar.LongitudClave).IsUnicode(false).IsFixedLength();

        builder.HasQueryFilter(FiltrosConsulta.BajaLogica, p => p.FechaEliminacion == null);
    }
}
