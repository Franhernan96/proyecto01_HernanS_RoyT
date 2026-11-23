using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeteoAula.Protocolo
{
    public static class ConstantesProtocolo
    {
        public const string Nombre = "MTP";
        public const int Version = 1;
        public const int PuertoPorDefecto = 8080;
        public const char SeparadorCampos = '|';
        public const char TerminadorMensaje = '\n';   // fin de mensaje (LF)
        public const int MaxBytesMensaje = 256;       // incluye el '\n' final
        public const int MaxRegistrosHistorial = 10;
        public const string FormatoFechaHora = "yyyy-MM-ddTHH:mm";

        // UTF-8 sin BOM
        public static readonly Encoding Codificacion = new UTF8Encoding(false);
    }
}
