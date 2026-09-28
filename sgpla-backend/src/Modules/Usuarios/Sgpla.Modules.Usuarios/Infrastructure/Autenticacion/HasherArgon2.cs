using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Isopoh.Cryptography.Argon2;
using Microsoft.Extensions.Options;
using Sgpla.Modules.Usuarios.Application.Autenticacion;

namespace Sgpla.Modules.Usuarios.Infrastructure.Autenticacion;

internal sealed partial class HasherArgon2(IOptions<Argon2Options> opciones) : IHasherContrasenas
{
    private const int LongitudSal = 16;
    private const int LongitudHash = 32;

    public string Hashear(string contrasena)
    {
        var actuales = opciones.Value;
        var sal = new byte[LongitudSal];
        RandomNumberGenerator.Fill(sal);

        var config = new Argon2Config
        {
            Type = Argon2Type.HybridAddressing,
            Version = Argon2Version.Nineteen,
            TimeCost = actuales.Iteraciones,
            MemoryCost = actuales.MemoriaKib,
            Lanes = actuales.Paralelismo,
            Threads = actuales.Paralelismo,
            Password = Encoding.UTF8.GetBytes(contrasena),
            Salt = sal,
            HashLength = LongitudHash,
        };

        return Argon2.Hash(config);
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Un verificador corrupto o ilegible se trata como contraseña incorrecta, no como falla técnica.")]
    public VerificacionContrasena Verificar(string verificador, string contrasena)
    {
        var parametros = ParametrosPhc().Match(verificador);
        if (!parametros.Success)
        {
            return VerificacionContrasena.Incorrecta;
        }

        bool correcta;
        try
        {
            correcta = Argon2.Verify(verificador, Encoding.UTF8.GetBytes(contrasena));
        }
        catch (Exception)
        {
            return VerificacionContrasena.Incorrecta;
        }

        if (!correcta)
        {
            return VerificacionContrasena.Incorrecta;
        }

        var actuales = opciones.Value;
        var memoria = int.Parse(parametros.Groups["m"].Value, CultureInfo.InvariantCulture);
        var iteraciones = int.Parse(parametros.Groups["t"].Value, CultureInfo.InvariantCulture);
        var paralelismo = int.Parse(parametros.Groups["p"].Value, CultureInfo.InvariantCulture);

        var requiereRehash = memoria < actuales.MemoriaKib
            || iteraciones < actuales.Iteraciones
            || paralelismo < actuales.Paralelismo;

        return requiereRehash ? VerificacionContrasena.CorrectaRequiereRehash : VerificacionContrasena.Correcta;
    }

    [GeneratedRegex(@"\$m=(?<m>\d+),t=(?<t>\d+),p=(?<p>\d+)\$")]
    private static partial Regex ParametrosPhc();
}
