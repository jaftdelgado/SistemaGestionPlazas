import { queryOptions } from "@tanstack/react-query";
import { client } from "@/shared/api/client";
import { unwrap, unwrapEmpty } from "@/shared/api/errors";
import type { components } from "@/shared/api/schema";
import type {
  CambioContrasena,
  Credenciales,
  SesionIniciada,
  SesionUsuario,
} from "../domain/sesion";

type UsuarioSesionResponse = components["schemas"]["UsuarioSesionResponse"];
type SesionResponse = components["schemas"]["SesionResponse"];

export const sesionKeys = {
  current: ["sesion", "actual"] as const,
};

// Un 401 no mejora al reintentar: la sesión ya no es válida.
export const sesionQuery = queryOptions({
  queryKey: sesionKeys.current,
  queryFn: async () =>
    toSesionUsuario(await unwrap(client.GET("/api/v1/usuarios/sesion"))),
  retry: false,
});

export async function iniciarSesion(
  credenciales: Credenciales,
): Promise<SesionIniciada> {
  const sesion = await unwrap(
    client.POST("/api/v1/usuarios/iniciar-sesion", { body: credenciales }),
  );
  return toSesionIniciada(sesion);
}

export async function cambiarContrasena(
  cambio: CambioContrasena,
): Promise<SesionIniciada> {
  const sesion = await unwrap(
    client.POST("/api/v1/usuarios/sesion/cambiar-contrasena", { body: cambio }),
  );
  return toSesionIniciada(sesion);
}

export function cerrarSesion(): Promise<void> {
  return unwrapEmpty(client.POST("/api/v1/usuarios/cerrar-sesion"));
}

function toSesionUsuario(dto: UsuarioSesionResponse): SesionUsuario {
  return {
    id: Number(dto.id),
    nombre: dto.nombre,
    correo: dto.correo,
    rol: { id: Number(dto.rol.id), nombre: dto.rol.nombre },
    cambioContrasenaPendiente: dto.cambioContrasenaPendiente,
  };
}

function toSesionIniciada(dto: SesionResponse): SesionIniciada {
  return {
    token: dto.token,
    expiraEn: dto.expiraEn,
    usuario: toSesionUsuario(dto.usuario),
  };
}
