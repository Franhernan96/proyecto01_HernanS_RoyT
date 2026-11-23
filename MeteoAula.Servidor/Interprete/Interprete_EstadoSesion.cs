using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeteoAula.Servidor.Interprete
{
    /// <summary>Estados de la sesión de un cliente (ver diagrama de estados).</summary>
    public enum EstadoSesion
    {
        Conectado,
        Identificado,
        Consultas,
        Cerrado
    }
}