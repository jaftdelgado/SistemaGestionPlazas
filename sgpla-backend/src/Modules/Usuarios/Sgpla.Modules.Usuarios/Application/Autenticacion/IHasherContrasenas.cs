namespace Sgpla.Modules.Usuarios.Application.Autenticacion;

internal enum VerificacionContrasena
{
    Incorrecta,
    Correcta,
    CorrectaRequiereRehash,
}

internal interface IHasherContrasenas
{
    /// <summary>Cadena PHC de Argon2id con una sal aleatoria de 16 bytes.</summary>
    string Hashear(string contrasena);

    /// <summary>Comparación en tiempo constante. Pide rehash si algún parámetro de la cadena está por debajo de la configuración.</summary>
    VerificacionContrasena Verificar(string verificador, string contrasena);
}
