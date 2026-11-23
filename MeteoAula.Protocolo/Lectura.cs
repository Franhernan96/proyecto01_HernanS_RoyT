using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeteoAula.Protocolo
{
    /// <summary>Una medición de una estación en un momento dado.</summary>
    public class Lectura
    {
        public DateTime FechaHora { get; private set; }
        public double Temperatura { get; private set; }   // grados Celsius
        public int Humedad { get; private set; }          // porcentaje, 0 a 100
        public string EstadoTiempo { get; private set; }  // SOLEADO, NUBLADO, LLUVIOSO, TORMENTA, NIEBLA

        public Lectura(DateTime fechaHora, double temperatura, int humedad, string estadoTiempo)
        {
            FechaHora = fechaHora;
            Temperatura = temperatura;
            Humedad = humedad;
            EstadoTiempo = estadoTiempo;
        }
    }
}