using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sgpla.Modules.Catalogos.Domain.Articulos;

namespace Sgpla.Modules.Catalogos.Infrastructure.Articulos;

internal sealed class ArticuloConfiguration : IEntityTypeConfiguration<Articulo>
{
    public void Configure(EntityTypeBuilder<Articulo> builder)
    {
        builder.ToTable("articulo", "plazas");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Numero).HasMaxLength(Articulo.LongitudMaximaNumero).IsUnicode(false);
        builder.Property(a => a.Descripcion).HasMaxLength(Articulo.LongitudMaximaDescripcion);
    }
}
