namespace Sgpla.Modules.OfertaEducativa.Domain.PlanesEstudio;

/// <summary>Datos de entrada de una EE, sin normalizar, tal como llegan en la importación o en el alta individual.</summary>
internal sealed record DatosExperienciaEducativa(
    string Nombre,
    string Materia,
    string Curso,
    int HorasTeoricas,
    int HorasPracticas,
    int Creditos,
    int? CupoMinimo,
    int? CupoMaximo,
    string? PerfilDocente,
    int AreaFormacionId);
