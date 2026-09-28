using Sgpla.SharedKernel;

namespace Sgpla.Modules.Institucional.Domain.AreasAcademicas;

internal static class AreaAcademicaErrors
{
    public static readonly Error ClaveNoPositiva = Error.Validation(
        "AreaAcademica.ClaveNoPositiva", "La clave debe ser mayor que cero.", nameof(AreaAcademica.Clave));

    public static readonly Error NombreVacio = Error.Validation(
        "AreaAcademica.NombreVacio", "El nombre es obligatorio.", nameof(AreaAcademica.Nombre));

    public static readonly Error NombreDemasiadoLargo = Error.Validation(
        "AreaAcademica.NombreDemasiadoLargo",
        $"El nombre admite hasta {AreaAcademica.LongitudMaximaNombre} caracteres.",
        nameof(AreaAcademica.Nombre));

    public static readonly Error TelefonoVacio = Error.Validation(
        "AreaAcademica.TelefonoVacio", "El teléfono es obligatorio.", nameof(AreaAcademica.Telefono));

    public static readonly Error TelefonoFormatoInvalido = Error.Validation(
        "AreaAcademica.TelefonoFormatoInvalido",
        "El teléfono debe tener exactamente diez dígitos, sin espacios ni separadores.",
        nameof(AreaAcademica.Telefono));

    public static readonly Error ExtensionFormatoInvalido = Error.Validation(
        "AreaAcademica.ExtensionFormatoInvalido",
        "La extensión admite de uno a diez dígitos.",
        nameof(AreaAcademica.Extension));

    public static readonly Error ClaveDuplicada = Error.Conflict(
        "AreaAcademica.ClaveDuplicada", "Ya existe un área académica con esa clave.");

    public static readonly Error TieneEntidadesActivas = Error.Conflict(
        "AreaAcademica.TieneEntidadesActivas", "El área académica tiene entidades académicas activas.");

    public static readonly Error TieneUsuariosActivos = Error.Conflict(
        "AreaAcademica.TieneUsuariosActivos", "El área académica tiene usuarios DGAA activos.");

    public static Error NoEncontrado(int id) => Error.NotFound(
        "AreaAcademica.NoEncontrado", $"No existe el área académica {id}.");
}
