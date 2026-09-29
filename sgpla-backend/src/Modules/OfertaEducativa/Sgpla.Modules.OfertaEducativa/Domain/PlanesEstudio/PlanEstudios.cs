using Sgpla.SharedKernel;

namespace Sgpla.Modules.OfertaEducativa.Domain.PlanesEstudio;

/// <summary>
/// Plan de un programa educativo, identificado por un código opaco (DATABASE.md §6.9). No tiene archivo ni campos
/// editables: el código y el programa son inmutables. Es el agregado que crea las EE: una EE solo nace dentro de su plan.
/// Con baja lógica y sin restauración (Modulo_OfertaEducativa.md, decisiones D4, D5 y D8).
/// </summary>
internal sealed class PlanEstudios : Entity, IEliminable
{
    public const int LongitudMaximaCodigo = 50;
    public const int MinimoExperienciasImportacion = 1;
    public const int MaximoExperienciasImportacion = 300;

    private readonly List<ExperienciaEducativa> _experienciasEducativas = [];

    private PlanEstudios()
    {
    }

    public string Codigo { get; private set; } = string.Empty;

    public int ProgramaEducativoId { get; private set; }

    public IReadOnlyCollection<ExperienciaEducativa> ExperienciasEducativas => _experienciasEducativas;

    public DateTime? FechaEliminacion { get; private set; }

    /// <summary>
    /// Valida el código, el número de EE y cada EE (el primer error lleva el campo prefijado con su índice), y que no haya
    /// dos con la misma materia y curso. Que el programa y las áreas existan lo comprueba el handler.
    /// </summary>
    public static Result<PlanEstudios> Crear(
        string codigo,
        int programaEducativoId,
        IReadOnlyList<DatosExperienciaEducativa> experiencias)
    {
        ArgumentNullException.ThrowIfNull(experiencias);

        var codigoNormalizado = NormalizarCodigo(codigo);
        if (codigoNormalizado.IsFailure)
        {
            return codigoNormalizado.Error;
        }

        if (experiencias.Count < MinimoExperienciasImportacion)
        {
            return PlanEstudiosErrors.SinExperiencias;
        }

        if (experiencias.Count > MaximoExperienciasImportacion)
        {
            return PlanEstudiosErrors.DemasiadasExperiencias;
        }

        var plan = new PlanEstudios { Codigo = codigoNormalizado.Value, ProgramaEducativoId = programaEducativoId };

        for (var i = 0; i < experiencias.Count; i++)
        {
            var creada = ExperienciaEducativa.Crear(experiencias[i]);
            if (creada.IsFailure)
            {
                return creada.Error with { Campo = $"{nameof(ExperienciasEducativas)}[{i}].{creada.Error.Campo}" };
            }

            plan._experienciasEducativas.Add(creada.Value);
        }

        var vistas = new HashSet<(string Materia, string Curso)>();
        for (var i = 0; i < plan._experienciasEducativas.Count; i++)
        {
            var experiencia = plan._experienciasEducativas[i];
            if (!vistas.Add((experiencia.Materia, experiencia.Curso)))
            {
                return PlanEstudiosErrors.ExperienciaRepetida(i);
            }
        }

        return plan;
    }

    /// <summary>Crea la EE y la agrega al plan. Que la materia y el curso no existan ya en la base lo comprueba el handler.</summary>
    public Result<ExperienciaEducativa> AgregarExperiencia(DatosExperienciaEducativa datos)
    {
        var creada = ExperienciaEducativa.Crear(datos);
        if (creada.IsFailure)
        {
            return creada.Error;
        }

        _experienciasEducativas.Add(creada.Value);
        return creada;
    }

    /// <summary>Da de baja el plan y las EE cargadas con el mismo instante (decisión D10). Idempotente.</summary>
    public void DarDeBaja(DateTime utc)
    {
        FechaEliminacion ??= utc;

        foreach (var experiencia in _experienciasEducativas)
        {
            experiencia.DarDeBaja(utc);
        }
    }

    /// <summary>"  isof-14-e-cr " → "ISOF-14-E-CR": en mayúsculas, solo A-Z, 0-9 y guiones.</summary>
    private static Result<string> NormalizarCodigo(string? codigo)
    {
        var normalizado = Normalizacion.Recortar(codigo).ToUpperInvariant();

        if (normalizado.Length == 0)
        {
            return PlanEstudiosErrors.CodigoVacio;
        }

        if (normalizado.Length > LongitudMaximaCodigo)
        {
            return PlanEstudiosErrors.CodigoDemasiadoLargo;
        }

        if (!normalizado.All(caracter => caracter is (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '-'))
        {
            return PlanEstudiosErrors.CodigoFormatoInvalido;
        }

        return normalizado;
    }
}
