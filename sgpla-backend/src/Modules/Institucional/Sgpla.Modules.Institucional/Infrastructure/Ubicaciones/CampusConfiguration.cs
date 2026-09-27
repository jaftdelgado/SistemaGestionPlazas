using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sgpla.Modules.Institucional.Domain.Ubicaciones;

namespace Sgpla.Modules.Institucional.Infrastructure.Ubicaciones;

internal sealed class CampusConfiguration : IEntityTypeConfiguration<Campus>
{
    public void Configure(EntityTypeBuilder<Campus> builder)
    {
        builder.ToTable("campus", "academico");
        builder.HasKey(c => c.Id);

        // varchar en la base: sin IsUnicode(false) los parámetros serían nvarchar y forzarían una conversión.
        builder.Property(c => c.Clave).HasMaxLength(Campus.LongitudMaximaClave).IsUnicode(false);
        builder.Property(c => c.Nombre).HasMaxLength(Campus.LongitudMaximaNombre);
    }
}
