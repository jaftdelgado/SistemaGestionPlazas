using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Sgpla.Modules.Usuarios.Infrastructure.Autenticacion;

/// <summary>Seguridad del canal LDAP.</summary>
internal enum SeguridadLdap
{
    Ldaps,
    StartTls,
    SinTls,
}

internal sealed class LdapOptions
{
    public const string Seccion = "Ldap";

    [Required]
    public string Servidor { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int Puerto { get; set; } = 636;

    public SeguridadLdap Seguridad { get; set; } = SeguridadLdap.Ldaps;

    [Range(1, 60)]
    public int TiempoEsperaSegundos { get; set; } = 10;
}

/// <summary><c>SinTls</c> solo se acepta en Development.</summary>
internal sealed class LdapOptionsValidador(IHostEnvironment entorno) : IValidateOptions<LdapOptions>
{
    public ValidateOptionsResult Validate(string? name, LdapOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.Seguridad == SeguridadLdap.SinTls && !entorno.IsDevelopment()
            ? ValidateOptionsResult.Fail("Ldap:Seguridad=SinTls solo se permite en el entorno Development.")
            : ValidateOptionsResult.Success;
    }
}
