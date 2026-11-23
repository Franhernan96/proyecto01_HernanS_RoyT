using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeteoAula.Cliente
{
    /// <summary>
    /// Una línea recibida del servidor ya interpretada:
    /// "212|2026-10-01T10:00|18.5|63|NUBLADO" -> Codigo = 212, Etiqueta = "",
    /// Campos = ["2026-10-01T10:00", "18.5", "63", "NUBLADO"]
    /// </summary>
    public class Respuesta
    {
        public int Codigo { get; private set; }
        public string Etiqueta { get; private set; }
        public List<string> Campos { get; private set; }
        public string TextoOriginal { get; private set; }

        public Respuesta(int codigo, string etiqueta, List<string> campos, string textoOriginal)
        {
            Codigo = codigo;
            Etiqueta = etiqueta;
            Campos = campos;
            TextoOriginal = textoOriginal;
        }

        // Los códigos 2xx indican éxito; 4xx y 5xx indican error
        public bool EsExito()
        {
            return Codigo >= 200 && Codigo < 300;
        }
    }
}
