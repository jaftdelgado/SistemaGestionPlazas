using Microsoft.Extensions.Options;
using Sgpla.Modules.SolicitudesApertura.Application.Periodos;

namespace Sgpla.Modules.SolicitudesApertura.Infrastructure.Periodos;

internal sealed class PeriodosConfigurados(IOptions<PeriodosOptions> opciones) : IPeriodosConfigurados
{
    public string ClaveActual => opciones.Value.PeriodoActual;

    public string ClaveSiguiente => opciones.Value.PeriodoSiguiente;
}
