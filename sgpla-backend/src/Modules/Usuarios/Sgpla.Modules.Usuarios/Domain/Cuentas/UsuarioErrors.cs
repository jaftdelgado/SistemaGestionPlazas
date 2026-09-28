using Sgpla.SharedKernel;

namespace Sgpla.Modules.Usuarios.Domain.Cuentas;

internal static class UsuarioErrors
{
    public static readonly Error CorreoVacio = Error.Validation(
        "Usuario.CorreoVacio", "El correo es obligatorio.", nameof(Usuario.Correo));

    public static readonly Error CorreoDemasiadoLargo = Error.Validation(
        "Usuario.CorreoDemasiadoLargo",
        $"El correo admite hasta {Usuario.LongitudMaximaCorreo} caracteres.",
        nameof(Usuario.Correo));

    public static readonly Error CorreoFormatoInvalido = Error.Validation(
        "Usuario.CorreoFormatoInvalido", "El correo no tiene un formato válido.", nameof(Usuario.Correo));

    public static readonly Error CorreoNoInstitucional = Error.Validation(
        "Usuario.CorreoNoInstitucional",
        "Las cuentas DGAA y Entidad Académica requieren un correo @uv.mx.",
        nameof(Usuario.Correo));

    public static readonly Error NombreVacio = Error.Validation(
        "Usuario.NombreVacio", "El nombre es obligatorio.", nameof(Usuario.Nombre));

    public static readonly Error NombreDemasiadoLargo = Error.Validation(
        "Usuario.NombreDemasiadoLargo",
        $"El nombre admite hasta {Usuario.LongitudMaximaNombre} caracteres.",
        nameof(Usuario.Nombre));

    public static readonly Error AreaAcademicaInexistente = Error.Validation(
        "Usuario.AreaAcademicaInexistente", "No existe un área académica activa con ese id.", "AreaAcademicaId");

    public static readonly Error EntidadAcademicaInexistente = Error.Validation(
        "Usuario.EntidadAcademicaInexistente", "No existe una entidad académica activa con ese id.", "EntidadAcademicaId");

    public static readonly Error ContrasenaActualIncorrecta = Error.Validation(
        "Usuario.ContrasenaActualIncorrecta", "La contraseña actual no es correcta.", "ContrasenaActual");

    public static readonly Error ContrasenaLongitudInvalida = Error.Validation(
        "Usuario.ContrasenaLongitudInvalida",
        $"La contraseña debe tener entre {PoliticaContrasena.LongitudMinima} y {PoliticaContrasena.LongitudMaxima} caracteres.",
        "ContrasenaNueva");

    public static readonly Error ContrasenaDebil = Error.Validation(
        "Usuario.ContrasenaDebil",
        "La contraseña debe incluir al menos una mayúscula, una minúscula, un número y un símbolo.",
        "ContrasenaNueva");

    public static readonly Error ContrasenaNuevaIgualActual = Error.Validation(
        "Usuario.ContrasenaNuevaIgualActual", "La contraseña nueva debe ser distinta de la actual.", "ContrasenaNueva");

    public static readonly Error CuentaNoRegistrada = Error.Unauthorized(
        "Usuario.CuentaNoRegistrada", "La cuenta no está registrada en el sistema.");

    public static readonly Error AmbitoInactivo = Error.Unauthorized(
        "Usuario.AmbitoInactivo", "El área o la entidad académica de la cuenta está dada de baja.");

    public static readonly Error CredencialesInvalidas = Error.Unauthorized(
        "Usuario.CredencialesInvalidas", "La contraseña es incorrecta.");

    public static readonly Error LdapNoDisponible = Error.Unavailable(
        "Usuario.LdapNoDisponible", "El servicio de autenticación de la UV no está disponible. Intenta más tarde.");

    public static readonly Error CorreoDuplicado = Error.Conflict(
        "Usuario.CorreoDuplicado", "Ya existe una cuenta activa con ese correo.");

    public static readonly Error BajaPropia = Error.Conflict(
        "Usuario.BajaPropia", "No puedes dar de baja tu propia cuenta.");

    public static readonly Error UltimoSuperusuario = Error.Conflict(
        "Usuario.UltimoSuperusuario", "No se puede dar de baja al último Superusuario activo.");

    public static readonly Error RestablecimientoPropio = Error.Conflict(
        "Usuario.RestablecimientoPropio", "No puedes restablecer tu propia contraseña; usa el cambio de contraseña.");

    public static readonly Error RestablecimientoNoAplica = Error.Conflict(
        "Usuario.RestablecimientoNoAplica",
        "Solo se restablece la contraseña de un Superusuario; las cuentas UV usan su contraseña institucional.");

    public static readonly Error CambioContrasenaNoAplica = Error.Conflict(
        "Usuario.CambioContrasenaNoAplica", "Las cuentas UV cambian su contraseña en los servicios de la UV.");

    public static readonly Error SuperusuarioExistente = Error.Conflict(
        "Usuario.SuperusuarioExistente", "Ya existe un Superusuario activo; no se creó nada.");

    public static Error NoEncontrado(int id) => Error.NotFound("Usuario.NoEncontrado", $"No existe la cuenta {id}.");
}
