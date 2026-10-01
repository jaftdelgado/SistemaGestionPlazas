using FluentValidation;
using Sgpla.BuildingBlocks.Application;

namespace Sgpla.Modules.SolicitudesApertura.Application.SolicitudesApertura;

internal sealed record SolicitudAperturaResponse(
    int Id,
    string Estado,
    string Seccion,
    int CantidadEstudiantes,
    string Justificacion,
    ExperienciaSolicitadaResponse ExperienciaEducativa,
    PeriodoSolicitadoResponse PeriodoEscolar,
    OficioResponse Oficio,
    DateTime CreadaEn,
    int CreadaPorUsuarioId,
    DateTime? ActualizadaEn,
    int? ActualizadaPorUsuarioId,
    DateTime? ResueltaEn,
    int? ResueltaPorUsuarioId,
    string? ComentariosResolucion,
    DateTime? CanceladaEn,
    int? CanceladaPorUsuarioId,
    string? MotivoCancelacion);

/// <summary>Datos derivados de la cadena académica de la EE; no se guardan en la solicitud.</summary>
internal sealed record ExperienciaSolicitadaResponse(
    int Id,
    string Materia,
    string Curso,
    string Nombre,
    int? CupoMinimo,
    int? CupoMaximo,
    int PlanEstudiosId,
    string PlanEstudiosCodigo,
    int ProgramaEducativoId,
    string ProgramaEducativoNombre,
    int EntidadAcademicaId,
    string EntidadAcademicaClave,
    string EntidadAcademicaNombre,
    int SistemaEducativoId,
    string Modalidad);

internal sealed record PeriodoSolicitadoResponse(int Id, string Clave);

internal sealed record OficioResponse(string Nombre, long Tamano, DateTime CargadoEn);

/// <summary>Filtros opcionales, combinados con AND.</summary>
internal sealed record FiltrosSolicitudesApertura(
    string? Estado,
    int? PeriodoEscolarId,
    int? ExperienciaEducativaId,
    int? EntidadAcademicaId);

/// <summary>Solicitudes del ámbito, de la más reciente a la más antigua (con el id como desempate), paginadas.</summary>
internal sealed record ListarSolicitudesAperturaQuery(Paginacion Paginacion, FiltrosSolicitudesApertura Filtros);

internal sealed class ListarSolicitudesAperturaValidator : AbstractValidator<ListarSolicitudesAperturaQuery>
{
    private static readonly string[] Estados = ["PENDIENTE", "ACEPTADA", "RECHAZADA", "CANCELADA"];

    public ListarSolicitudesAperturaValidator()
    {
        Include(new PaginacionValidator<ListarSolicitudesAperturaQuery>(q => q.Paginacion));

        RuleFor(q => q.Filtros.Estado)
            .Must(estado => estado is null || Estados.Contains(estado, StringComparer.OrdinalIgnoreCase))
            .OverridePropertyName("estado")
            .WithName("estado")
            .When(q => q.Filtros.Estado is not null);
        RuleFor(q => q.Filtros.PeriodoEscolarId).GreaterThan(0)
            .OverridePropertyName("periodoEscolarId").WithName("periodo escolar")
            .When(q => q.Filtros.PeriodoEscolarId is not null);
        RuleFor(q => q.Filtros.ExperienciaEducativaId).GreaterThan(0)
            .OverridePropertyName("experienciaEducativaId").WithName("experiencia educativa")
            .When(q => q.Filtros.ExperienciaEducativaId is not null);
        RuleFor(q => q.Filtros.EntidadAcademicaId).GreaterThan(0)
            .OverridePropertyName("entidadAcademicaId").WithName("entidad académica")
            .When(q => q.Filtros.EntidadAcademicaId is not null);
    }
}

internal sealed record ObtenerSolicitudAperturaQuery(int Id);

internal sealed record DescargarOficioSolicitudAperturaQuery(int Id);

/// <summary>El contenido lo cierra quien escribe la respuesta.</summary>
internal sealed record OficioDescarga(Stream Contenido, string Nombre);
