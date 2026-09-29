using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sgpla.Modules.Catalogos.Domain.CatalogosFijos;

namespace Sgpla.Modules.Catalogos.Infrastructure.CatalogosFijos;

/// <summary>
/// Mapeo común de un catálogo fijo: tabla, clave y longitud del nombre. Las columnas adicionales
/// (<c>requiere_lugar</c>, <c>grado_academico_id</c>) salen de la convención snake_case.
/// </summary>
internal abstract class CatalogoFijoConfiguration<TCatalogo>(string esquema, string tabla, int longitudMaximaNombre)
    : IEntityTypeConfiguration<TCatalogo>
    where TCatalogo : CatalogoFijo
{
    public void Configure(EntityTypeBuilder<TCatalogo> builder)
    {
        builder.ToTable(tabla, esquema);
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Nombre).HasMaxLength(longitudMaximaNombre);
    }
}

internal sealed class MunicipioConfiguration()
    : CatalogoFijoConfiguration<Municipio>("academico", "municipio", Municipio.LongitudMaximaNombre);

internal sealed class GradoAcademicoConfiguration()
    : CatalogoFijoConfiguration<GradoAcademico>("academico", "grado_academico", GradoAcademico.LongitudMaximaNombre);

internal sealed class TipoDocumentoExpedienteConfiguration()
    : CatalogoFijoConfiguration<TipoDocumentoExpediente>(
        "academico", "tipo_documento_expediente", TipoDocumentoExpediente.LongitudMaximaNombre);

internal sealed class SistemaEducativoConfiguration()
    : CatalogoFijoConfiguration<SistemaEducativo>("academico", "sistema_educativo", SistemaEducativo.LongitudMaximaNombre);

internal sealed class NivelFormacionConfiguration()
    : CatalogoFijoConfiguration<NivelFormacion>("academico", "nivel_formacion", NivelFormacion.LongitudMaximaNombre);

internal sealed class AreaFormacionConfiguration()
    : CatalogoFijoConfiguration<AreaFormacion>("academico", "area_formacion", AreaFormacion.LongitudMaximaNombre);

internal sealed class TipoPlazaConfiguration()
    : CatalogoFijoConfiguration<TipoPlaza>("plazas", "tipo_plaza", TipoPlaza.LongitudMaximaNombre);

internal sealed class TipoContratacionConfiguration()
    : CatalogoFijoConfiguration<TipoContratacion>("plazas", "tipo_contratacion", TipoContratacion.LongitudMaximaNombre);

internal sealed class ModalidadRecepcionConfiguration()
    : CatalogoFijoConfiguration<ModalidadRecepcion>("plazas", "modalidad_recepcion", ModalidadRecepcion.LongitudMaximaNombre);

internal sealed class TratamientoAcademicoConfiguration()
    : CatalogoFijoConfiguration<TratamientoAcademico>(
        "plazas", "tratamiento_academico", TratamientoAcademico.LongitudMaximaNombre);
