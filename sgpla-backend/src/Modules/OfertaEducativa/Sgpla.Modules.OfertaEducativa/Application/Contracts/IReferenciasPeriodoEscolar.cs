namespace Sgpla.Modules.OfertaEducativa.Application.Contracts;

/// <summary>
/// Lo implementan los módulos que referencian periodos escolares (Integracion, SolicitudesApertura y Publicacion, P8).
/// Cualquier referencia bloquea la baja del periodo (Modulo_OfertaEducativa.md, D11).
/// </summary>
public interface IReferenciasPeriodoEscolar
{
    Task<bool> TieneReferenciasAsync(int periodoEscolarId, CancellationToken cancellationToken);
}
