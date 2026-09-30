using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sgpla.Modules.OfertaEducativa.Domain.Programaciones;

namespace Sgpla.Modules.OfertaEducativa.Infrastructure.Programaciones;

internal sealed class HorarioProgramacionConfiguration : IEntityTypeConfiguration<HorarioProgramacion>
{
    public void Configure(EntityTypeBuilder<HorarioProgramacion> builder)
    {
        builder.ToTable("horario_programacion", "academico");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Edificio).HasMaxLength(HorarioProgramacion.LongitudMaximaEspacio);
        builder.Property(h => h.Aula).HasMaxLength(HorarioProgramacion.LongitudMaximaEspacio);
    }
}
