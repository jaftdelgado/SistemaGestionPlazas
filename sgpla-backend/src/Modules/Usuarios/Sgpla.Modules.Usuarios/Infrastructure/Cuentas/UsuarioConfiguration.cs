using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Usuarios.Domain.Cuentas;

namespace Sgpla.Modules.Usuarios.Infrastructure.Cuentas;

internal sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuario", "usuarios");
        builder.HasQueryFilter(FiltrosConsulta.BajaLogica, u => u.FechaEliminacion == null);

        builder.Property(u => u.Correo).HasMaxLength(Usuario.LongitudMaximaCorreo).IsUnicode(false);
        builder.Property(u => u.Nombre).HasMaxLength(Usuario.LongitudMaximaNombre);
        builder.Property(u => u.Rol).HasColumnName("rol_id").HasConversion<byte>();

        builder.OwnsOne(u => u.PerfilDgaa, dgaa =>
        {
            dgaa.ToTable("usuario_dgaa", "usuarios");
            dgaa.WithOwner().HasForeignKey("usuario_id");
            dgaa.Property(p => p.AreaAcademicaId).HasColumnName("area_academica_id");
        });

        builder.OwnsOne(u => u.PerfilEntidadAcademica, entidad =>
        {
            entidad.ToTable("usuario_entidad_academica", "usuarios");
            entidad.WithOwner().HasForeignKey("usuario_id");
            entidad.Property(p => p.EntidadAcademicaId).HasColumnName("entidad_academica_id");
        });

        builder.OwnsOne(u => u.Credencial, credencial =>
        {
            credencial.ToTable("credencial_superusuario", "usuarios");
            credencial.WithOwner().HasForeignKey("usuario_id");
            credencial.Property(c => c.Contrasena)
                .HasColumnName("contrasena")
                .HasMaxLength(CredencialSuperusuario.LongitudMaximaContrasena)
                .IsUnicode(false);
            credencial.Property(c => c.FechaActualizacion).HasColumnName("fecha_actualizacion");
            credencial.Property(c => c.FechaEliminacion).HasColumnName("fecha_eliminacion");
        });
    }
}
