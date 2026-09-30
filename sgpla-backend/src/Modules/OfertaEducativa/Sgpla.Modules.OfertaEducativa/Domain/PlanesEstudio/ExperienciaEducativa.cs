using Sgpla.SharedKernel;

namespace Sgpla.Modules.OfertaEducativa.Domain.PlanesEstudio;

/// <summary>
/// EE exclusiva de un plan, identificada dentro de él por materia y curso. Nace con su plan o con un
/// alta individual. Las horas, los créditos y el área de formación se congelan con la primera programación, incluidas
/// las dadas de baja. Con baja lógica y sin restauración.
/// </summary>
internal sealed class ExperienciaEducativa : Entity, IEliminable
{
    public const int LongitudMaximaNombre = 200;
    public const int LongitudMaximaMateria = 50;
    public const int LongitudMaximaCurso = 50;

    private ExperienciaEducativa()
    {
    }

    public string Nombre { get; private set; } = string.Empty;

    public string Materia { get; private set; } = string.Empty;

    public string Curso { get; private set; } = string.Empty;

    public int HorasTeoricas { get; private set; }

    public int HorasPracticas { get; private set; }

    public int Creditos { get; private set; }

    public int? CupoMinimo { get; private set; }

    public int? CupoMaximo { get; private set; }

    public string? PerfilDocente { get; private set; }

    public int AreaFormacionId { get; private set; }

    public int PlanEstudiosId { get; private set; }

    public DateTime? FechaEliminacion { get; private set; }

    /// <summary>
    /// La usa solo <see cref="PlanEstudios"/>. Valida en este orden: nombre, materia, curso, horas teóricas, horas
    /// prácticas, créditos y cupos; que el área exista lo comprueba el handler.
    /// </summary>
    internal static Result<ExperienciaEducativa> Crear(DatosExperienciaEducativa datos)
    {
        ArgumentNullException.ThrowIfNull(datos);

        var nombre = NormalizarNombre(datos.Nombre);
        if (nombre.IsFailure)
        {
            return nombre.Error;
        }

        var materia = NormalizarClave(
            datos.Materia,
            ExperienciaEducativaErrors.MateriaVacia,
            ExperienciaEducativaErrors.MateriaDemasiadoLarga,
            ExperienciaEducativaErrors.MateriaFormatoInvalido,
            LongitudMaximaMateria);
        if (materia.IsFailure)
        {
            return materia.Error;
        }

        var curso = NormalizarClave(
            datos.Curso,
            ExperienciaEducativaErrors.CursoVacio,
            ExperienciaEducativaErrors.CursoDemasiadoLargo,
            ExperienciaEducativaErrors.CursoFormatoInvalido,
            LongitudMaximaCurso);
        if (curso.IsFailure)
        {
            return curso.Error;
        }

        var cantidades = ValidarCantidades(
            datos.HorasTeoricas, datos.HorasPracticas, datos.Creditos, datos.CupoMinimo, datos.CupoMaximo);
        if (cantidades.IsFailure)
        {
            return cantidades.Error;
        }

        return new ExperienciaEducativa
        {
            Nombre = nombre.Value,
            Materia = materia.Value,
            Curso = curso.Value,
            HorasTeoricas = datos.HorasTeoricas,
            HorasPracticas = datos.HorasPracticas,
            Creditos = datos.Creditos,
            CupoMinimo = datos.CupoMinimo,
            CupoMaximo = datos.CupoMaximo,
            PerfilDocente = NormalizarPerfil(datos.PerfilDocente),
            AreaFormacionId = datos.AreaFormacionId,
        };
    }

    /// <summary>
    /// Reemplazo completo: un cupo o un perfil en <c>null</c> los quita. Con <paramref name="tuvoProgramaciones"/>, las
    /// horas, los créditos y el área no pueden cambiar. No asigna nada si algo falla.
    /// </summary>
    public Result Modificar(
        string nombre,
        int horasTeoricas,
        int horasPracticas,
        int creditos,
        int? cupoMinimo,
        int? cupoMaximo,
        string? perfilDocente,
        int areaFormacionId,
        bool tuvoProgramaciones)
    {
        var nombreNormalizado = NormalizarNombre(nombre);
        if (nombreNormalizado.IsFailure)
        {
            return nombreNormalizado.Error;
        }

        var cantidades = ValidarCantidades(horasTeoricas, horasPracticas, creditos, cupoMinimo, cupoMaximo);
        if (cantidades.IsFailure)
        {
            return cantidades.Error;
        }

        if (tuvoProgramaciones
            && (horasTeoricas != HorasTeoricas
                || horasPracticas != HorasPracticas
                || creditos != Creditos
                || areaFormacionId != AreaFormacionId))
        {
            return ExperienciaEducativaErrors.AtributosCurricularesInmutables;
        }

        Nombre = nombreNormalizado.Value;
        HorasTeoricas = horasTeoricas;
        HorasPracticas = horasPracticas;
        Creditos = creditos;
        CupoMinimo = cupoMinimo;
        CupoMaximo = cupoMaximo;
        PerfilDocente = NormalizarPerfil(perfilDocente);
        AreaFormacionId = areaFormacionId;
        return Result.Success();
    }

    public void DarDeBaja(DateTime utc) => FechaEliminacion ??= utc;

    private static Result<string> NormalizarNombre(string? nombre)
    {
        var normalizado = Normalizacion.Texto(nombre);

        if (normalizado.Length == 0)
        {
            return ExperienciaEducativaErrors.NombreVacio;
        }

        if (normalizado.Length > LongitudMaximaNombre)
        {
            return ExperienciaEducativaErrors.NombreDemasiadoLargo;
        }

        return normalizado;
    }

    /// <summary>"  enso " → "ENSO"; conserva los ceros iniciales (<c>00001</c>).</summary>
    private static Result<string> NormalizarClave(
        string? valor,
        Error vacia,
        Error demasiadoLarga,
        Error formatoInvalido,
        int longitudMaxima)
    {
        var normalizado = Normalizacion.Recortar(valor).ToUpperInvariant();

        if (normalizado.Length == 0)
        {
            return vacia;
        }

        if (normalizado.Length > longitudMaxima)
        {
            return demasiadoLarga;
        }

        if (!normalizado.All(caracter => caracter is (>= 'A' and <= 'Z') or (>= '0' and <= '9')))
        {
            return formatoInvalido;
        }

        return normalizado;
    }

    private static Result ValidarCantidades(int horasTeoricas, int horasPracticas, int creditos, int? cupoMinimo, int? cupoMaximo)
    {
        if (horasTeoricas < 0)
        {
            return ExperienciaEducativaErrors.HorasTeoricasNegativas;
        }

        if (horasPracticas < 0)
        {
            return ExperienciaEducativaErrors.HorasPracticasNegativas;
        }

        if (creditos <= 0)
        {
            return ExperienciaEducativaErrors.CreditosNoPositivos;
        }

        if (cupoMinimo < 0)
        {
            return ExperienciaEducativaErrors.CupoMinimoNegativo;
        }

        if (cupoMaximo < 0)
        {
            return ExperienciaEducativaErrors.CupoMaximoNegativo;
        }

        if (cupoMinimo > cupoMaximo)
        {
            return ExperienciaEducativaErrors.CuposInvertidos;
        }

        return Result.Success();
    }

    /// <summary>Recorta; vacío es <c>null</c>. No colapsa espacios ni saltos de línea internos.</summary>
    private static string? NormalizarPerfil(string? perfil)
    {
        var recortado = Normalizacion.Recortar(perfil);
        return recortado.Length == 0 ? null : recortado;
    }
}
