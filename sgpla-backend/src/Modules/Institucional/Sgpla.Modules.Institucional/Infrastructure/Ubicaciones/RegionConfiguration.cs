using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sgpla.Modules.Institucional.Domain.Ubicaciones;

namespace Sgpla.Modules.Institucional.Infrastructure.Ubicaciones;

internal sealed class RegionConfiguration : IEntityTypeConfiguration<Region>
{
    public void Configure(EntityTypeBuilder<Region> builder)
    {
        builder.ToTable("region", "academico");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Nombre).HasMaxLength(Region.LongitudMaximaNombre);
    }
}
