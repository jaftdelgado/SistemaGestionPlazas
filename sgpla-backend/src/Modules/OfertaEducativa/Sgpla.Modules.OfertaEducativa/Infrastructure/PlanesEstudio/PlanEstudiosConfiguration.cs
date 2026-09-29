using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.OfertaEducativa.Domain.PlanesEstudio;

namespace Sgpla.Modules.OfertaEducativa.Infrastructure.PlanesEstudio;

internal sealed class PlanEstudiosConfiguration : IEntityTypeConfiguration<PlanEstudios>
{
    public void Configure(EntityTypeBuilder<PlanEstudios> builder)
    {
        builder.ToTable("plan_estudios", "academico");
        builder.HasKey(p => p.Id);

        // varchar en la base.
        builder.Property(p => p.Codigo).HasMaxLength(PlanEstudios.LongitudMaximaCodigo).IsUnicode(false);

        // La lista privada es la fuente; la propiedad pública solo la expone de solo lectura.
        builder.HasMany(p => p.ExperienciasEducativas).WithOne().HasForeignKey(e => e.PlanEstudiosId);
        builder.Navigation(p => p.ExperienciasEducativas)
            .HasField("_experienciasEducativas")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasQueryFilter(FiltrosConsulta.BajaLogica, p => p.FechaEliminacion == null);
    }
}
