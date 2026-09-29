using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.OfertaEducativa.Domain.PlanesEstudio;

namespace Sgpla.Modules.OfertaEducativa.Infrastructure.ExperienciasEducativas;

internal sealed class ExperienciaEducativaConfiguration : IEntityTypeConfiguration<ExperienciaEducativa>
{
    public void Configure(EntityTypeBuilder<ExperienciaEducativa> builder)
    {
        builder.ToTable("experiencia_educativa", "academico");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Nombre).HasMaxLength(ExperienciaEducativa.LongitudMaximaNombre);

        // Las columnas se llaman materia_ee y curso_ee, y son varchar.
        builder.Property(e => e.Materia)
            .HasColumnName("materia_ee").HasMaxLength(ExperienciaEducativa.LongitudMaximaMateria).IsUnicode(false);
        builder.Property(e => e.Curso)
            .HasColumnName("curso_ee").HasMaxLength(ExperienciaEducativa.LongitudMaximaCurso).IsUnicode(false);

        // nvarchar(max): sin longitud.
        builder.Property(e => e.PerfilDocente);

        builder.HasQueryFilter(FiltrosConsulta.BajaLogica, e => e.FechaEliminacion == null);
    }
}
