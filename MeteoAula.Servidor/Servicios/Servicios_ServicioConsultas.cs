using System.Collections.Generic;
using MeteoAula.Protocolo;
using MeteoAula.Servidor.Datos;

namespace MeteoAula.Servidor.Servicios
{
    /// <summary>
    /// Lógica de consultas. Trabaja con objetos (Estacion, Lectura...),
    /// no con texto del protocolo ni con sockets.
    /// </summary>
    public class ServicioConsultas
    {
        private IRepositorioLecturas repositorio;

        public ServicioConsultas(IRepositorioLecturas repositorio)
        {
            this.repositorio = repositorio;
        }

        public List<Estacion> ObtenerEstaciones()
        {
            return repositorio.ObtenerEstaciones();
        }

        public bool ExisteEstacion(string codigo)
        {
            return repositorio.BuscarEstacion(Normalizar(codigo)) != null;
        }

        /// <summary>Lectura de fecha más reciente, o null si no hay lecturas.</summary>
        public Lectura ObtenerLecturaActual(string codigo)
        {
            List<Lectura> lecturas = repositorio.ObtenerLecturas(Normalizar(codigo));

            Lectura masReciente = null;
            foreach (Lectura lectura in lecturas)
            {
                if (masReciente == null || lectura.FechaHora > masReciente.FechaHora)
                {
                    masReciente = lectura;
                }
            }
            return masReciente;
        }

        /// <summary>
        /// Las N lecturas más recientes, en orden cronológico ascendente
        /// (la más antigua primero). Si hay menos de N, devuelve las que existan.
        /// </summary>
        public List<Lectura> ObtenerHistorial(string codigo, int n)
        {
            List<Lectura> lecturas = repositorio.ObtenerLecturas(Normalizar(codigo));

            // Se ordena de la más antigua a la más reciente
            lecturas.Sort(CompararPorFecha);

            // Se toman las últimas N
            int inicio = lecturas.Count - n;
            if (inicio < 0)
            {
                inicio = 0;
            }
            return lecturas.GetRange(inicio, lecturas.Count - inicio);
        }

        /// <summary>Promedio, mínimo y máximo de temperatura, o null si no hay lecturas.</summary>
        public ResumenTemperatura ObtenerResumen(string codigo)
        {
            List<Lectura> lecturas = repositorio.ObtenerLecturas(Normalizar(codigo));

            if (lecturas.Count == 0)
            {
                return null;
            }

            double suma = 0;
            double minimo = lecturas[0].Temperatura;
            double maximo = lecturas[0].Temperatura;

            foreach (Lectura lectura in lecturas)
            {
                suma = suma + lectura.Temperatura;
                if (lectura.Temperatura < minimo)
                {
                    minimo = lectura.Temperatura;
                }
                if (lectura.Temperatura > maximo)
                {
                    maximo = lectura.Temperatura;
                }
            }

            double promedio = suma / lecturas.Count;
            return new ResumenTemperatura(promedio, minimo, maximo, lecturas.Count);
        }

        // Los códigos de estación no distinguen mayúsculas de minúsculas
        private string Normalizar(string codigo)
        {
            return codigo.ToUpperInvariant();
        }

        private static int CompararPorFecha(Lectura a, Lectura b)
        {
            return a.FechaHora.CompareTo(b.FechaHora);
        }
    }
}