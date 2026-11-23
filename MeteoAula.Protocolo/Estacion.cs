using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeteoAula.Protocolo
{
    /// <summary>Una estación meteorológica del observatorio.</summary>
    public class Estacion
    {
        public string Codigo { get; private set; }
        public string Nombre { get; private set; }
        public string Ciudad { get; private set; }

        public Estacion(string codigo, string nombre, string ciudad)
        {
            Codigo = codigo;
            Nombre = nombre;
            Ciudad = ciudad;
        }
    }
}
