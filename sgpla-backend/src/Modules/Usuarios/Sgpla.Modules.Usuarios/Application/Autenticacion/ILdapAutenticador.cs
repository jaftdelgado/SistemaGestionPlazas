namespace Sgpla.Modules.Usuarios.Application.Autenticacion;

internal enum ResultadoLdap
{
    Autenticado,
    CredencialesInvalidas,
    NoDisponible,
}

internal interface ILdapAutenticador
{
    Task<ResultadoLdap> AutenticarAsync(string correo, string contrasena, CancellationToken cancellationToken);
}
