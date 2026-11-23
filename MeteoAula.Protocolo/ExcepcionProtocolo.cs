using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeteoAula.Protocolo
{
    /// <summary>
    /// Se lanza cuando una solicitud viola el protocolo (comando desconocido,
    /// formato inválido, mensaje muy largo, etc.). Lleva el código de error
    /// que se enviará al cliente.
    /// </summary>
    public class ExcepcionProtocolo : Exception
    {
        public int Codigo { get; private set; }

        public ExcepcionProtocolo(int codigo, string mensaje) : base(mensaje)
        {
            Codigo = codigo;
        }
    }
}
