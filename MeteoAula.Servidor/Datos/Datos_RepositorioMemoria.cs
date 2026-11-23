using System;
using System.Collections.Generic;
using MeteoAula.Protocolo;

namespace MeteoAula.Servidor.Datos
{
    /// <summary>
    /// Datos precargados en memoria: 5 estaciones con 12 lecturas cada una
    /// (una por hora, de 08:00 a 19:00 del 2026-10-01).
    /// </summary>
    public class RepositorioMemoria : IRepositorioLecturas
    {
        private List<Estacion> estaciones;
        private Dictionary<string, List<Lectura>> lecturasPorEstacion;

        public RepositorioMemoria()
        {
            estaciones = new List<Estacion>();
            lecturasPorEstacion = new Dictionary<string, List<Lectura>>();
            CargarEstaciones();
            CargarLecturas();
        }

        private void CargarEstaciones()
        {
            estaciones.Add(new Estacion("UIO", "Mariscal Sucre", "Quito"));
            estaciones.Add(new Estacion("GYE", "José Joaquín de Olmedo", "Guayaquil"));
            estaciones.Add(new Estacion("CUE", "Mariscal Lamar", "Cuenca"));
            estaciones.Add(new Estacion("MEC", "Eloy Alfaro", "Manta"));
            estaciones.Add(new Estacion("LTX", "Cotopaxi", "Latacunga"));
        }

        private void CargarLecturas()
        {
            AgregarLecturas("UIO",
                new double[] { 16.8, 17.6, 18.5, 19.2, 20.1, 21.0, 21.4, 20.8, 19.9, 18.7, 17.9, 17.2 },
                new int[] { 68, 66, 63, 60, 57, 54, 52, 55, 59, 63, 66, 68 },
                new string[] { "NIEBLA", "NUBLADO", "NUBLADO", "SOLEADO", "SOLEADO", "SOLEADO",
                               "SOLEADO", "NUBLADO", "NUBLADO", "LLUVIOSO", "LLUVIOSO", "NUBLADO" });

            AgregarLecturas("GYE",
                new double[] { 26.5, 27.3, 28.4, 29.6, 30.8, 31.5, 32.0, 31.2, 30.1, 29.0, 28.2, 27.6 },
                new int[] { 82, 80, 77, 73, 70, 67, 65, 68, 72, 76, 79, 81 },
                new string[] { "NUBLADO", "NUBLADO", "SOLEADO", "SOLEADO", "SOLEADO", "SOLEADO",
                               "SOLEADO", "NUBLADO", "NUBLADO", "LLUVIOSO", "LLUVIOSO", "NUBLADO" });

            AgregarLecturas("CUE",
                new double[] { 12.4, 13.1, 14.0, 15.2, 16.3, 17.1, 17.6, 17.0, 16.1, 15.0, 14.2, 13.5 },
                new int[] { 79, 77, 74, 70, 66, 63, 61, 64, 68, 72, 75, 77 },
                new string[] { "NIEBLA", "NUBLADO", "NUBLADO", "NUBLADO", "SOLEADO", "SOLEADO",
                               "SOLEADO", "NUBLADO", "LLUVIOSO", "LLUVIOSO", "NUBLADO", "NIEBLA" });

            AgregarLecturas("MEC",
                new double[] { 25.2, 25.9, 26.8, 27.9, 28.7, 29.3, 29.8, 29.1, 28.3, 27.4, 26.6, 26.0 },
                new int[] { 84, 83, 80, 77, 74, 72, 70, 73, 76, 79, 82, 83 },
                new string[] { "NUBLADO", "NUBLADO", "NUBLADO", "SOLEADO", "SOLEADO", "SOLEADO",
                               "SOLEADO", "SOLEADO", "NUBLADO", "LLUVIOSO", "LLUVIOSO", "NUBLADO" });

            AgregarLecturas("LTX",
                new double[] { 9.8, 10.5, 11.6, 12.9, 14.0, 14.8, 15.2, 14.5, 13.4, 12.2, 11.0, 10.3 },
                new int[] { 72, 70, 67, 63, 59, 56, 54, 58, 62, 66, 69, 71 },
                new string[] { "NIEBLA", "NIEBLA", "NUBLADO", "NUBLADO", "SOLEADO", "SOLEADO",
                               "SOLEADO", "NUBLADO", "NUBLADO", "LLUVIOSO", "TORMENTA", "NUBLADO" });
        }

        // Crea una lectura por hora a partir de las 08:00.
        // Los tres arreglos deben tener la misma cantidad de elementos.
        private void AgregarLecturas(string codigo, double[] temperaturas, int[] humedades, string[] estados)
        {
            List<Lectura> lista = new List<Lectura>();
            DateTime inicio = new DateTime(2026, 10, 1, 8, 0, 0);

            for (int i = 0; i < temperaturas.Length; i++)
            {
                lista.Add(new Lectura(inicio.AddHours(i), temperaturas[i], humedades[i], estados[i]));
            }

            lecturasPorEstacion.Add(codigo, lista);
        }

        public List<Estacion> ObtenerEstaciones()
        {
            return new List<Estacion>(estaciones);
        }

        public Estacion BuscarEstacion(string codigo)
        {
            foreach (Estacion estacion in estaciones)
            {
                if (estacion.Codigo == codigo)
                {
                    return estacion;
                }
            }
            return null;
        }

        public List<Lectura> ObtenerLecturas(string codigo)
        {
            if (lecturasPorEstacion.ContainsKey(codigo))
            {
                // Se devuelve una copia para que quien la use no altere los datos originales
                return new List<Lectura>(lecturasPorEstacion[codigo]);
            }
            return new List<Lectura>();
        }
    }
}