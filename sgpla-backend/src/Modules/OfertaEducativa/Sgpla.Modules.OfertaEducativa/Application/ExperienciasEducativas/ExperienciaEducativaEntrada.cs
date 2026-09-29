using Sgpla.Modules.OfertaEducativa.Domain.PlanesEstudio;

namespace Sgpla.Modules.OfertaEducativa.Application.ExperienciasEducativas;

/// <summary>
/// Datos de una EE tal como llegan en el cuerpo de la importación o del alta individual, sin normalizar. Los endpoints
/// la enlazan directamente porque no pueden usar los tipos de Domain; el handler la convierte con <see cref="ADatos"/>.
/// </summary>
internal sealed record ExperienciaEducativaEntrada(
    string Nombre,
    string Materia,
    string Curso,
    int HorasTeoricas,
    int HorasPracticas,
    int Creditos,
    int? CupoMinimo,
    int? CupoMaximo,
    string? PerfilDocente,
    int AreaFormacionId)
{
    public DatosExperienciaEducativa ADatos() => new(
        Nombre,
        Materia,
        Curso,
        HorasTeoricas,
        HorasPracticas,
        Creditos,
        CupoMinimo,
        CupoMaximo,
        PerfilDocente,
        AreaFormacionId);
}
