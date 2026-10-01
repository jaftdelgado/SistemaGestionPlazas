using System.Security.Cryptography;
using Microsoft.Data.SqlClient;

namespace Sgpla.IntegrationTests.SolicitudesApertura;

internal static class DatosSolicitudesSql
{
    public static async Task<int> InsertarAsync(
        string cadenaConexion,
        int experienciaEducativaId,
        int periodoEscolarId,
        string estado,
        string seccion,
        DateTime? creadaEn = null)
    {
        await using var conexion = new SqlConnection(cadenaConexion);
        await conexion.OpenAsync(TestContext.Current.CancellationToken);
        await using var transaccion = await conexion.BeginTransactionAsync(TestContext.Current.CancellationToken);

        await using var comandoArchivo = conexion.CreateCommand();
        comandoArchivo.Transaction = (SqlTransaction)transaccion;
        comandoArchivo.CommandText = """
            INSERT INTO academico.archivo_solicitud_apertura
                (nombre, mime, tamano, checksum_sha256, clave_almacenamiento, cargado_en, cargado_por_usuario_id)
            VALUES (@nombre, 'application/pdf', @tamano, @checksum, @clave, '2026-10-01T15:04:05',
                (SELECT MIN(id) FROM usuarios.usuario));
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;
        comandoArchivo.Parameters.AddWithValue("@nombre", $"{Guid.NewGuid():N}.pdf");
        comandoArchivo.Parameters.AddWithValue("@tamano", Pdf.LongLength);
        comandoArchivo.Parameters.AddWithValue("@checksum", SHA256.HashData(Pdf));
        comandoArchivo.Parameters.AddWithValue("@clave", $"solicitudes-apertura/{Guid.NewGuid():N}.pdf");
        var archivoId = (int)(await comandoArchivo.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;

        await using var comando = conexion.CreateCommand();
        comando.Transaction = (SqlTransaction)transaccion;
        comando.CommandText = """
            INSERT INTO academico.solicitud_apertura
                (experiencia_educativa_id, periodo_escolar_id, seccion, cantidad_estudiantes, justificacion,
                 oficio_respaldo_id, estado, creada_en, creada_por_usuario_id, resuelta_en,
                 resuelta_por_usuario_id, comentarios_resolucion, cancelada_en, cancelada_por_usuario_id,
                 motivo_cancelacion)
            VALUES (@experienciaId, @periodoId, @seccion, 20, N'Solicitud de prueba.', @archivoId, @estado,
                @creadaEn, (SELECT MIN(id) FROM usuarios.usuario), @resueltaEn,
                CASE WHEN @estado IN ('ACEPTADA', 'RECHAZADA') THEN (SELECT MIN(id) FROM usuarios.usuario) END,
                @comentarios, @canceladaEn,
                CASE WHEN @estado = 'CANCELADA' THEN (SELECT MIN(id) FROM usuarios.usuario) END,
                @motivo);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;
        comando.Parameters.AddWithValue("@experienciaId", experienciaEducativaId);
        comando.Parameters.AddWithValue("@periodoId", periodoEscolarId);
        comando.Parameters.AddWithValue("@seccion", seccion);
        comando.Parameters.AddWithValue("@archivoId", archivoId);
        comando.Parameters.AddWithValue("@estado", estado);
        comando.Parameters.Add("@creadaEn", System.Data.SqlDbType.DateTime2).Value = creadaEn ?? Fecha;
        comando.Parameters.Add("@resueltaEn", System.Data.SqlDbType.DateTime2).Value =
            estado is "ACEPTADA" or "RECHAZADA" ? Fecha : DBNull.Value;
        comando.Parameters.Add("@comentarios", System.Data.SqlDbType.NVarChar, -1).Value =
            estado == "RECHAZADA" ? "Solicitud rechazada." : DBNull.Value;
        comando.Parameters.Add("@canceladaEn", System.Data.SqlDbType.DateTime2).Value =
            estado == "CANCELADA" ? Fecha : DBNull.Value;
        comando.Parameters.Add("@motivo", System.Data.SqlDbType.NVarChar, 1000).Value =
            estado == "CANCELADA" ? "Cancelación de prueba." : DBNull.Value;
        var solicitudId = (int)(await comando.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;

        await transaccion.CommitAsync(TestContext.Current.CancellationToken);
        return solicitudId;
    }

    private static readonly byte[] Pdf = "%PDF-prueba"u8.ToArray();

    private static readonly DateTime Fecha = new(2026, 10, 1, 15, 4, 5, DateTimeKind.Utc);
}
