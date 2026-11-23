using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeteoAula.Protocolo
{
    /// <summary>
    /// Una solicitud del cliente ya separada en nombre y parámetros.
    /// Ejemplo: "HISTORY|UIO|2" -> Nombre = "HISTORY", Parametros = ["UIO", "2"]
    /// </summary>
    public class Comando
    {
        public string Nombre { get; private set; }
        public List<string> Parametros { get; private set; }

        public Comando(string nombre, List<string> parametros)
        {
            Nombre = nombre;
            Parametros = parametros;
        }
    }
}