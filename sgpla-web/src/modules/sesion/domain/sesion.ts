export type SesionUsuario = {
  id: number;
  nombre: string;
  correo: string;
  rol: { id: number; nombre: string };
  cambioContrasenaPendiente: boolean;
};

export type SesionIniciada = {
  token: string;
  expiraEn: string;
  usuario: SesionUsuario;
};

export type Credenciales = { correo: string; contrasena: string };

export type CambioContrasena = {
  contrasenaActual: string;
  contrasenaNueva: string;
};
