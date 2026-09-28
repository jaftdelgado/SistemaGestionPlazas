namespace Sgpla.Modules.Usuarios.Application.Autenticacion;

internal interface IGeneradorContrasenas
{
    /// <summary>12 caracteres que cumplen <see cref="Domain.Cuentas.PoliticaContrasena"/>.</summary>
    string GenerarTemporal();
}
