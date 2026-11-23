using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeteoAula.Protocolo
{
    /// <summary>Promedio, mínimo y máximo de temperatura de una estación.</summary>
    public class ResumenTemperatura
    {
        public double Promedio { get; private set; }
        public double Minimo { get; private set; }
        public double Maximo { get; private set; }
        public int CantidadLecturas { get; private set; }

        public ResumenTemperatura(double promedio, double minimo, double maximo, int cantidadLecturas)
        {
            Promedio = promedio;
            Minimo = minimo;
            Maximo = maximo;
            CantidadLecturas = cantidadLecturas;
        }
    }
}
