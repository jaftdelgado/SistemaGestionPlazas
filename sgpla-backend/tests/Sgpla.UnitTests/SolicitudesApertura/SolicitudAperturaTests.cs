using Sgpla.Modules.SolicitudesApertura.Domain.SolicitudesApertura;
using Sgpla.SharedKernel;

namespace Sgpla.UnitTests.SolicitudesApertura;

public sealed class SolicitudAperturaTests
{
    [Fact]
    public void Datos_CreaNormalizaSeccionYConservaSaltosDeLineaInternos()
    {
        var justificacion = "  Primera línea.\n\nSegunda línea.  ";

        var datos = DatosSolicitudApertura.Crear(" a2 ", 25, justificacion).Value;

        datos.Seccion.ShouldBe("A2");
        datos.Justificacion.ShouldBe("Primera línea.\n\nSegunda línea.");
    }

    [Theory]
    [InlineData(null, 25, "Texto", "SolicitudApertura.SeccionVacia", "Seccion")]
    [InlineData("   ", 25, "Texto", "SolicitudApertura.SeccionVacia", "Seccion")]
    [InlineData("A123456789012345678901", 25, "Texto", "SolicitudApertura.SeccionDemasiadoLarga", "Seccion")]
    [InlineData("A 2", 25, "Texto", "SolicitudApertura.SeccionFormatoInvalido", "Seccion")]
    [InlineData("Á2", 25, "Texto", "SolicitudApertura.SeccionFormatoInvalido", "Seccion")]
    [InlineData("A2", 0, "Texto", "SolicitudApertura.CantidadNoPositiva", "CantidadEstudiantes")]
    [InlineData("A2", 25, "  ", "SolicitudApertura.JustificacionVacia", "Justificacion")]
    public void Datos_ConDatoInvalidoDevuelvePrimerErrorYCampo(
        string? seccion,
        int cantidad,
        string justificacion,
        string codigo,
        string campo)
    {
        var resultado = DatosSolicitudApertura.Crear(seccion, cantidad, justificacion);

        resultado.Error.Code.ShouldBe(codigo);
        resultado.Error.Campo.ShouldBe(campo);
    }

    [Fact]
    public void Datos_ConJustificacionDeMasDeDosMilCaracteres_FallaEnJustificacion()
    {
        var resultado = DatosSolicitudApertura.Crear("A2", 25, new string('x', 2001));

        resultado.Error.ShouldBe(SolicitudAperturaErrors.JustificacionDemasiadoLarga);
        resultado.Error.Campo.ShouldBe("Justificacion");
    }

    [Fact]
    public void Crear_NacePendienteConActorYFecha()
    {
        var datos = DatosSolicitudApertura.Crear("A2", 25, "Solicitud válida.").Value;
        var archivo = ArchivoSolicitudApertura.Crear("oficio.pdf", 5, new byte[32], "solicitudes-apertura/a.pdf", 44, Fecha);

        var solicitud = SolicitudApertura.Crear(datos, 301, 99, archivo, 44, Fecha);

        solicitud.Estado.ShouldBe(EstadoSolicitudApertura.Pendiente);
        solicitud.CreadaPorUsuarioId.ShouldBe(44);
        solicitud.CreadaEn.ShouldBe(Fecha);
    }

    [Theory]
    [InlineData(12, null, null, true)]
    [InlineData(10, 10, null, true)]
    [InlineData(40, null, 40, true)]
    [InlineData(10, 10, 40, true)]
    [InlineData(40, 10, 40, true)]
    [InlineData(9, 10, 40, false)]
    [InlineData(41, 10, 40, false)]
    public void ValidarCupos_AdmiteSoloLosValoresDentroDeLosLimites(
        int cantidad,
        int? minimo,
        int? maximo,
        bool valido)
    {
        var resultado = SolicitudApertura.ValidarCupos(cantidad, minimo, maximo);

        resultado.IsSuccess.ShouldBe(valido);
        if (!valido)
        {
            resultado.Error.ShouldBe(SolicitudAperturaErrors.CantidadFueraDeCupos);
        }
    }

    [Fact]
    public void Modificar_ConDatosValidos_AsignaCamposYAuditoria()
    {
        var solicitud = NuevaSolicitud();

        var resultado = solicitud.Modificar(30, "  Nueva justificación.\n\nConserva saltos.  ", 10, 40, 77, Posterior);

        resultado.IsSuccess.ShouldBeTrue();
        solicitud.CantidadEstudiantes.ShouldBe(30);
        solicitud.Justificacion.ShouldBe("Nueva justificación.\n\nConserva saltos.");
        solicitud.ActualizadaEn.ShouldBe(Posterior);
        solicitud.ActualizadaPorUsuarioId.ShouldBe(77);
        solicitud.Estado.ShouldBe(EstadoSolicitudApertura.Pendiente);
    }

    [Theory]
    [InlineData("Pendiente", 0, "Texto", 10, 40, "SolicitudApertura.CantidadNoPositiva", "CantidadEstudiantes")]
    [InlineData("Pendiente", -3, "  ", 10, 40, "SolicitudApertura.CantidadNoPositiva", "CantidadEstudiantes")]
    [InlineData("Pendiente", 30, "  ", 10, 40, "SolicitudApertura.JustificacionVacia", "Justificacion")]
    [InlineData("Pendiente", 30, null, 10, 40, "SolicitudApertura.JustificacionVacia", "Justificacion")]
    [InlineData("Aceptada", 30, "  ", 10, 40, "SolicitudApertura.JustificacionVacia", "Justificacion")]
    [InlineData("Aceptada", 50, "Texto", 10, 40, "SolicitudApertura.NoPendiente", null)]
    [InlineData("Rechazada", 30, "Texto", 10, 40, "SolicitudApertura.NoPendiente", null)]
    [InlineData("Cancelada", 30, "Texto", 10, 40, "SolicitudApertura.NoPendiente", null)]
    [InlineData("Pendiente", 9, "Texto", 10, 40, "SolicitudApertura.CantidadFueraDeCupos", null)]
    [InlineData("Pendiente", 41, "Texto", 10, 40, "SolicitudApertura.CantidadFueraDeCupos", null)]
    public void Modificar_ConError_DevuelveElPrimeroEnSuOrdenYNoAsignaNada(
        string estado,
        int cantidad,
        string? justificacion,
        int? minimo,
        int? maximo,
        string codigo,
        string? campo)
    {
        var solicitud = SolicitudEn(estado);
        var antes = Instantanea(solicitud);

        var resultado = solicitud.Modificar(cantidad, justificacion, minimo, maximo, 77, Posterior);

        resultado.Error.Code.ShouldBe(codigo);
        resultado.Error.Campo.ShouldBe(campo);
        Instantanea(solicitud).ShouldBe(antes);
    }

    [Fact]
    public void Modificar_ConJustificacionDeMasDeDosMilCaracteres_FallaEnJustificacion()
    {
        var solicitud = NuevaSolicitud();
        var antes = Instantanea(solicitud);

        var resultado = solicitud.Modificar(30, new string('x', 2001), 10, 40, 77, Posterior);

        resultado.Error.ShouldBe(SolicitudAperturaErrors.JustificacionDemasiadoLarga);
        Instantanea(solicitud).ShouldBe(antes);
    }

    [Fact]
    public void ReemplazarOficio_AsignaElNuevoYDevuelveElAnterior()
    {
        var solicitud = NuevaSolicitud();
        var anterior = solicitud.Oficio;
        var nuevo = ArchivoSolicitudApertura.Crear("nuevo.pdf", 9, new byte[32], "solicitudes-apertura/b.pdf", 44, Posterior);

        var devuelto = solicitud.ReemplazarOficio(nuevo);

        devuelto.ShouldBeSameAs(anterior);
        solicitud.Oficio.ShouldBeSameAs(nuevo);
    }

    [Theory]
    [InlineData("  Visto bueno.  ", "Visto bueno.")]
    [InlineData("   ", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void Aceptar_ConComentarios_LosRecortaYLosVaciosQuedanNulos(string? comentarios, string? esperado)
    {
        var solicitud = NuevaSolicitud();

        var resultado = solicitud.Aceptar(comentarios, 10, 40, 88, Posterior);

        resultado.IsSuccess.ShouldBeTrue();
        solicitud.Estado.ShouldBe(EstadoSolicitudApertura.Aceptada);
        solicitud.ComentariosResolucion.ShouldBe(esperado);
        solicitud.ResueltaEn.ShouldBe(Posterior);
        solicitud.ResueltaPorUsuarioId.ShouldBe(88);
        solicitud.CanceladaEn.ShouldBeNull();
    }

    [Theory]
    [InlineData("Pendiente", 'L', 10, 40, "SolicitudApertura.ComentariosDemasiadoLargos", "Comentarios")]
    [InlineData("Aceptada", 'L', 10, 40, "SolicitudApertura.ComentariosDemasiadoLargos", "Comentarios")]
    [InlineData("Aceptada", 'C', null, null, "SolicitudApertura.NoPendiente", null)]
    [InlineData("Rechazada", 'C', 10, 40, "SolicitudApertura.NoPendiente", null)]
    [InlineData("Cancelada", 'C', 10, 40, "SolicitudApertura.NoPendiente", null)]
    [InlineData("Pendiente", 'C', null, null, "SolicitudApertura.CuposIncompletos", null)]
    [InlineData("Pendiente", 'C', 10, null, "SolicitudApertura.CuposIncompletos", null)]
    [InlineData("Pendiente", 'C', null, 40, "SolicitudApertura.CuposIncompletos", null)]
    [InlineData("Pendiente", 'C', 26, 40, "SolicitudApertura.CantidadFueraDeCupos", null)]
    [InlineData("Pendiente", 'C', 10, 24, "SolicitudApertura.CantidadFueraDeCupos", null)]
    public void Aceptar_ConError_DevuelveElPrimeroEnSuOrdenYNoAsignaNada(
        string estado,
        char comentarios,
        int? minimo,
        int? maximo,
        string codigo,
        string? campo)
    {
        var solicitud = SolicitudEn(estado);
        var antes = Instantanea(solicitud);

        var resultado = solicitud.Aceptar(Comentarios(comentarios, 2001), minimo, maximo, 88, Posterior);

        resultado.Error.Code.ShouldBe(codigo);
        resultado.Error.Campo.ShouldBe(campo);
        Instantanea(solicitud).ShouldBe(antes);
    }

    [Theory]
    [InlineData(10, 25)]
    [InlineData(25, 40)]
    public void Aceptar_ConCantidadEnLosBordesDelRango_Acepta(int minimo, int maximo)
    {
        var solicitud = NuevaSolicitud();

        solicitud.Aceptar(null, minimo, maximo, 88, Posterior).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Rechazar_ConComentarios_PasaARechazadaConActorYFecha()
    {
        var solicitud = NuevaSolicitud();

        var resultado = solicitud.Rechazar("  No hay profesor disponible.  ", 88, Posterior);

        resultado.IsSuccess.ShouldBeTrue();
        solicitud.Estado.ShouldBe(EstadoSolicitudApertura.Rechazada);
        solicitud.ComentariosResolucion.ShouldBe("No hay profesor disponible.");
        solicitud.ResueltaEn.ShouldBe(Posterior);
        solicitud.ResueltaPorUsuarioId.ShouldBe(88);
    }

    [Theory]
    [InlineData("Pendiente", null, "SolicitudApertura.ComentariosVacios", "Comentarios")]
    [InlineData("Pendiente", "", "SolicitudApertura.ComentariosVacios", "Comentarios")]
    [InlineData("Pendiente", "   ", "SolicitudApertura.ComentariosVacios", "Comentarios")]
    [InlineData("Aceptada", "   ", "SolicitudApertura.ComentariosVacios", "Comentarios")]
    [InlineData("Pendiente", "L", "SolicitudApertura.ComentariosDemasiadoLargos", "Comentarios")]
    [InlineData("Aceptada", "Texto", "SolicitudApertura.NoPendiente", null)]
    [InlineData("Rechazada", "Texto", "SolicitudApertura.NoPendiente", null)]
    [InlineData("Cancelada", "Texto", "SolicitudApertura.NoPendiente", null)]
    public void Rechazar_ConError_DevuelveElPrimeroEnSuOrdenYNoAsignaNada(
        string estado,
        string? comentarios,
        string codigo,
        string? campo)
    {
        var solicitud = SolicitudEn(estado);
        var antes = Instantanea(solicitud);

        var resultado = solicitud.Rechazar(comentarios == "L" ? new string('x', 2001) : comentarios, 88, Posterior);

        resultado.Error.Code.ShouldBe(codigo);
        resultado.Error.Campo.ShouldBe(campo);
        Instantanea(solicitud).ShouldBe(antes);
    }

    [Fact]
    public void Cancelar_ConMotivo_PasaACanceladaConActorYFecha()
    {
        var solicitud = NuevaSolicitud();

        var resultado = solicitud.Cancelar("  Ya no se necesita.  ", 44, Posterior);

        resultado.IsSuccess.ShouldBeTrue();
        solicitud.Estado.ShouldBe(EstadoSolicitudApertura.Cancelada);
        solicitud.MotivoCancelacion.ShouldBe("Ya no se necesita.");
        solicitud.CanceladaEn.ShouldBe(Posterior);
        solicitud.CanceladaPorUsuarioId.ShouldBe(44);
        solicitud.ResueltaEn.ShouldBeNull();
    }

    [Theory]
    [InlineData("Pendiente", null, "SolicitudApertura.MotivoVacio", "Motivo")]
    [InlineData("Pendiente", "   ", "SolicitudApertura.MotivoVacio", "Motivo")]
    [InlineData("Aceptada", "   ", "SolicitudApertura.MotivoVacio", "Motivo")]
    [InlineData("Pendiente", "L", "SolicitudApertura.MotivoDemasiadoLargo", "Motivo")]
    [InlineData("Aceptada", "L", "SolicitudApertura.MotivoDemasiadoLargo", "Motivo")]
    [InlineData("Aceptada", "Texto", "SolicitudApertura.NoPendiente", null)]
    [InlineData("Rechazada", "Texto", "SolicitudApertura.NoPendiente", null)]
    [InlineData("Cancelada", "Texto", "SolicitudApertura.NoPendiente", null)]
    public void Cancelar_ConError_DevuelveElPrimeroEnSuOrdenYNoAsignaNada(
        string estado,
        string? motivo,
        string codigo,
        string? campo)
    {
        var solicitud = SolicitudEn(estado);
        var antes = Instantanea(solicitud);

        var resultado = solicitud.Cancelar(motivo == "L" ? new string('x', 1001) : motivo, 44, Posterior);

        resultado.Error.Code.ShouldBe(codigo);
        resultado.Error.Campo.ShouldBe(campo);
        Instantanea(solicitud).ShouldBe(antes);
    }

    [Fact]
    public void Cancelar_ConMotivoDeMilCaracteres_Acepta()
    {
        var solicitud = NuevaSolicitud();

        solicitud.Cancelar(new string('x', 1000), 44, Posterior).IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Aceptada")]
    [InlineData("Rechazada")]
    [InlineData("Cancelada")]
    public void CualquierTransicionDesdeUnEstadoTerminal_DevuelveNoPendiente(string estado)
    {
        var solicitud = SolicitudEn(estado);
        var antes = Instantanea(solicitud);

        var resultados = new[]
        {
            solicitud.Modificar(30, "Texto", 10, 40, 77, Posterior),
            solicitud.Aceptar(null, 10, 40, 88, Posterior),
            solicitud.Rechazar("Texto", 88, Posterior),
            solicitud.Cancelar("Texto", 44, Posterior),
        };

        resultados.ShouldAllBe(resultado => resultado.IsFailure && resultado.Error == SolicitudAperturaErrors.NoPendiente);
        Instantanea(solicitud).ShouldBe(antes);
    }

    private static readonly DateTime Fecha = new(2026, 10, 1, 15, 4, 5, DateTimeKind.Utc);

    private static readonly DateTime Posterior = new(2026, 10, 2, 9, 30, 0, DateTimeKind.Utc);

    private static string Comentarios(char tipo, int largo) => tipo == 'L' ? new string('x', largo) : "Comentarios.";

    private static SolicitudApertura NuevaSolicitud()
    {
        var datos = DatosSolicitudApertura.Crear("A2", 25, "Solicitud válida.").Value;
        var archivo = ArchivoSolicitudApertura.Crear("oficio.pdf", 5, new byte[32], "solicitudes-apertura/a.pdf", 44, Fecha);
        return SolicitudApertura.Crear(datos, 301, 99, archivo, 44, Fecha);
    }

    /// <summary>Lleva una solicitud nueva al estado pedido con los propios métodos de la entidad.</summary>
    private static SolicitudApertura SolicitudEn(string estado)
    {
        var solicitud = NuevaSolicitud();
        var resultado = estado switch
        {
            "Aceptada" => solicitud.Aceptar("Aceptada.", 10, 40, 88, Fecha),
            "Rechazada" => solicitud.Rechazar("Rechazada.", 88, Fecha),
            "Cancelada" => solicitud.Cancelar("Cancelada.", 44, Fecha),
            _ => Result.Success(),
        };
        resultado.IsSuccess.ShouldBeTrue();
        return solicitud;
    }

    private static (int, string, ArchivoSolicitudApertura, EstadoSolicitudApertura, DateTime?, int?, DateTime?, int?, string?, DateTime?, int?, string?) Instantanea(
        SolicitudApertura s) =>
        (
            s.CantidadEstudiantes,
            s.Justificacion,
            s.Oficio,
            s.Estado,
            s.ActualizadaEn,
            s.ActualizadaPorUsuarioId,
            s.ResueltaEn,
            s.ResueltaPorUsuarioId,
            s.ComentariosResolucion,
            s.CanceladaEn,
            s.CanceladaPorUsuarioId,
            s.MotivoCancelacion);
}
