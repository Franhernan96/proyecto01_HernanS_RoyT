using System.Collections.Generic;
using MeteoAula.Protocolo;

namespace MeteoAula.Servidor.Datos
{
    public interface IRepositorioLecturas
    {
        List<Estacion> ObtenerEstaciones();

        // Devuelve null si la estación no existe
        Estacion BuscarEstacion(string codigo);

        // Devuelve las lecturas de la estación (lista vacía si no existe),
        // en orden cronológico ascendente
        List<Lectura> ObtenerLecturas(string codigo);
    }
}