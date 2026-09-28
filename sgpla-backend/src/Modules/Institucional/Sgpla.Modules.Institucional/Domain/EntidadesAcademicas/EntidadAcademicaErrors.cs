using Sgpla.SharedKernel;

namespace Sgpla.Modules.Institucional.Domain.EntidadesAcademicas;

internal static class EntidadAcademicaErrors
{
    public static readonly Error ClaveVacia = Error.Validation(
        "EntidadAcademica.ClaveVacia", "La clave es obligatoria.", nameof(EntidadAcademica.Clave));

    public static readonly Error ClaveDemasiadoLarga = Error.Validation(
        "EntidadAcademica.ClaveDemasiadoLarga",
        $"La clave admite hasta {EntidadAcademica.LongitudMaximaClave} caracteres.",
        nameof(EntidadAcademica.Clave));

    public static readonly Error ClaveFormatoInvalido = Error.Validation(
        "EntidadAcademica.ClaveFormatoInvalido",
        "La clave solo admite letras de la A a la Z sin acentos y dígitos.",
        nameof(EntidadAcademica.Clave));

    public static readonly Error NombreVacio = Error.Validation(
        "EntidadAcademica.NombreVacio", "El nombre es obligatorio.", nameof(EntidadAcademica.Nombre));

    public static readonly Error NombreDemasiadoLargo = Error.Validation(
        "EntidadAcademica.NombreDemasiadoLargo",
        $"El nombre admite hasta {EntidadAcademica.LongitudMaximaNombre} caracteres.",
        nameof(EntidadAcademica.Nombre));

    public static readonly Error CalleVacia = Error.Validation(
        "EntidadAcademica.CalleVacia", "La calle es obligatoria.", nameof(EntidadAcademica.Calle));

    public static readonly Error CalleDemasiadoLarga = Error.Validation(
        "EntidadAcademica.CalleDemasiadoLarga",
        $"La calle admite hasta {EntidadAcademica.LongitudMaximaCalle} caracteres.",
        nameof(EntidadAcademica.Calle));

    public static readonly Error NumeroExteriorVacio = Error.Validation(
        "EntidadAcademica.NumeroExteriorVacio",
        "El número exterior no puede estar vacío; omítelo si el inmueble no tiene número.",
        nameof(EntidadAcademica.NumeroExterior));

    public static readonly Error NumeroExteriorDemasiadoLargo = Error.Validation(
        "EntidadAcademica.NumeroExteriorDemasiadoLargo",
        $"El número exterior admite hasta {EntidadAcademica.LongitudMaximaNumeroExterior} caracteres.",
        nameof(EntidadAcademica.NumeroExterior));

    public static readonly Error ColoniaVacia = Error.Validation(
        "EntidadAcademica.ColoniaVacia", "La colonia es obligatoria.", nameof(EntidadAcademica.Colonia));

    public static readonly Error ColoniaDemasiadoLarga = Error.Validation(
        "EntidadAcademica.ColoniaDemasiadoLarga",
        $"La colonia admite hasta {EntidadAcademica.LongitudMaximaColonia} caracteres.",
        nameof(EntidadAcademica.Colonia));

    public static readonly Error CodigoPostalVacio = Error.Validation(
        "EntidadAcademica.CodigoPostalVacio", "El código postal es obligatorio.", nameof(EntidadAcademica.CodigoPostal));

    public static readonly Error CodigoPostalFormatoInvalido = Error.Validation(
        "EntidadAcademica.CodigoPostalFormatoInvalido",
        "El código postal debe tener exactamente cinco dígitos.",
        nameof(EntidadAcademica.CodigoPostal));

    public static readonly Error TelefonoVacio = Error.Validation(
        "EntidadAcademica.TelefonoVacio", "El teléfono es obligatorio.", nameof(EntidadAcademica.Telefono));

    public static readonly Error TelefonoFormatoInvalido = Error.Validation(
        "EntidadAcademica.TelefonoFormatoInvalido",
        "El teléfono debe tener exactamente diez dígitos, sin espacios ni separadores.",
        nameof(EntidadAcademica.Telefono));

    public static readonly Error ExtensionFormatoInvalido = Error.Validation(
        "EntidadAcademica.ExtensionFormatoInvalido",
        "La extensión admite de uno a diez dígitos.",
        nameof(EntidadAcademica.Extension));

    public static readonly Error CampusInexistente = Error.Validation(
        "EntidadAcademica.CampusInexistente", "No existe el campus indicado.", nameof(EntidadAcademica.CampusId));

    public static readonly Error AreaAcademicaInexistente = Error.Validation(
        "EntidadAcademica.AreaAcademicaInexistente",
        "No existe un área académica activa con ese id.",
        nameof(EntidadAcademica.AreaAcademicaId));

    public static readonly Error MunicipioInexistente = Error.Validation(
        "EntidadAcademica.MunicipioInexistente",
        "No existe el municipio indicado.",
        nameof(EntidadAcademica.MunicipioId));

    public static readonly Error ClaveDuplicada = Error.Conflict(
        "EntidadAcademica.ClaveDuplicada", "Ya existe una entidad académica con esa clave.");

    public static readonly Error TieneUsuariosActivos = Error.Conflict(
        "EntidadAcademica.TieneUsuariosActivos", "La entidad académica tiene usuarios activos.");

    public static Error NoEncontrado(int id) => Error.NotFound(
        "EntidadAcademica.NoEncontrado", $"No existe la entidad académica {id}.");
}
