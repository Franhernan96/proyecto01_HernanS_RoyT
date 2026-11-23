using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeteoAula.Protocolo
{
    public static class NombresComandos
    {
        public const string Hello = "HELLO";
        public const string Help = "HELP";
        public const string List = "LIST";
        public const string Current = "CURRENT";
        public const string History = "HISTORY";
        public const string Summary = "SUMMARY";
        public const string Quit = "QUIT";
    }

    public static class CodigosRespuesta
    {
        // Éxito
        public const int Bienvenida = 200;
        public const int Despedida = 201;
        public const int Ayuda = 202;
        public const int LecturaActual = 203;
        public const int Resumen = 204;
        public const int Conteo = 210;
        public const int FilaEstacion = 211;
        public const int FilaLectura = 212;
        public const int Fin = 219;

        // Errores del cliente
        public const int ComandoDesconocido = 400;
        public const int FaltanParametros = 401;
        public const int FormatoInvalido = 402;
        public const int MensajeMuyLargo = 403;
        public const int EstacionNoExiste = 404;
        public const int FueraDeRango = 405;
        public const int EstadoInvalido = 406;
        public const int VersionNoSoportada = 407;

        // Error del servidor
        public const int ErrorInterno = 500;

        // Devuelve la etiqueta de texto que acompaña a cada código en la respuesta
        // (por ejemplo 404 -> "STATION_NOT_FOUND"). Las filas de datos (211 y 212)
        // no llevan etiqueta.
        public static string ObtenerEtiqueta(int codigo)
        {
            switch (codigo)
            {
                case Bienvenida: return "WELCOME";
                case Despedida: return "BYE";
                case Ayuda: return "HELP";
                case LecturaActual: return "CURRENT";
                case Resumen: return "SUMMARY";
                case Conteo: return "COUNT";
                case Fin: return "END";
                case ComandoDesconocido: return "UNKNOWN_COMMAND";
                case FaltanParametros: return "MISSING_PARAMS";
                case FormatoInvalido: return "INVALID_FORMAT";
                case MensajeMuyLargo: return "MESSAGE_TOO_LONG";
                case EstacionNoExiste: return "STATION_NOT_FOUND";
                case FueraDeRango: return "OUT_OF_RANGE";
                case EstadoInvalido: return "INVALID_STATE";
                case VersionNoSoportada: return "UNSUPPORTED_VERSION";
                case ErrorInterno: return "INTERNAL_ERROR";
                default: return "UNKNOWN";
            }
        }
    }
}
