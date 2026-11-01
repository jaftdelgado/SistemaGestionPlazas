export type EntidadAcademica = {
  id: number;
  nombre: string;
  areaAcademicaId: number;
};

// "Facultad de Estadística e Informática" se muestra como "Fac. de Estadística e Informática".
export function abbreviateFacultad(name: string): string {
  return name.replace(/\bFacultad\b/g, "Fac.");
}
