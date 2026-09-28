using Sgpla.Modules.Usuarios.Domain.Cuentas;

namespace Sgpla.Modules.Usuarios.Application.Autenticacion;

internal sealed record TokenEmitido(string Token, DateTime ExpiraEn);

internal interface IEmisorTokens
{
    TokenEmitido Emitir(Usuario usuario);
}
