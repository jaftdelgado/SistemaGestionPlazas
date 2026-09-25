using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sgpla.Modules.Catalogos.Domain.GradosAcademicos;

namespace Sgpla.Modules.Catalogos.Infrastructure.GradosAcademicos;

internal sealed class GradoAcademicoConfiguration : IEntityTypeConfiguration<GradoAcademico>
{
    public void Configure(EntityTypeBuilder<GradoAcademico> builder)
    {
        builder.ToTable("grado_academico", "academico");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Nombre).HasMaxLength(GradoAcademico.LongitudMaximaNombre);
    }
}
