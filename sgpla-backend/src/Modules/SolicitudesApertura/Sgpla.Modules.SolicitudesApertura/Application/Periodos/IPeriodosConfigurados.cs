namespace Sgpla.Modules.SolicitudesApertura.Application.Periodos;

/// <summary>Pareja de periodos que fija la configuración para recibir solicitudes.</summary>
internal interface IPeriodosConfigurados
{
    string ClaveActual { get; }

    string ClaveSiguiente { get; }
}
