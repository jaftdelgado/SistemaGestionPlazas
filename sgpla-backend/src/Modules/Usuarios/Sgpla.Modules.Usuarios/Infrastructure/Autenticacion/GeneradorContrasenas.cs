using System.Security.Cryptography;
using Sgpla.Modules.Usuarios.Application.Autenticacion;

namespace Sgpla.Modules.Usuarios.Infrastructure.Autenticacion;

/// <summary>12 caracteres sin ambigüedad visual, con al menos uno de cada alfabeto.</summary>
internal sealed class GeneradorContrasenas : IGeneradorContrasenas
{
    private const int Longitud = 12;
    private const string Mayusculas = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Minusculas = "abcdefghijkmnopqrstuvwxyz";
    private const string Digitos = "23456789";
    private const string Simbolos = "!@#$%&*?-_=+";

    public string GenerarTemporal()
    {
        var alfabetos = new[] { Mayusculas, Minusculas, Digitos, Simbolos };
        var union = string.Concat(alfabetos);

        var caracteres = new char[Longitud];
        for (var i = 0; i < alfabetos.Length; i++)
        {
            caracteres[i] = ElegirCaracter(alfabetos[i]);
        }

        for (var i = alfabetos.Length; i < Longitud; i++)
        {
            caracteres[i] = ElegirCaracter(union);
        }

        Mezclar(caracteres);
        return new string(caracteres);
    }

    private static char ElegirCaracter(string alfabeto) => alfabeto[RandomNumberGenerator.GetInt32(alfabeto.Length)];

    private static void Mezclar(char[] caracteres)
    {
        for (var i = caracteres.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (caracteres[i], caracteres[j]) = (caracteres[j], caracteres[i]);
        }
    }
}
