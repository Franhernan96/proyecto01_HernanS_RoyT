using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace MeteoAula.Protocolo
{
    /// <summary>
    /// Convierte fechas y temperaturas a texto y viceversa con el formato del protocolo.
    /// Se usa CultureInfo.InvariantCulture para que el separador decimal sea SIEMPRE
    /// el punto (18.5), sin importar la configuración regional de Windows.
    /// Servidor y cliente usan esta misma clase, así nunca se desincronizan.
    /// </summary>
    public static class ConvertidorDatos
    {
        public static string FechaHoraATexto(DateTime fechaHora)
        {
            return fechaHora.ToString(ConstantesProtocolo.FormatoFechaHora, CultureInfo.InvariantCulture);
        }

        // Un decimal: 18.5, 19.1, 30.0
        public static string TemperaturaATexto(double temperatura)
        {
            return temperatura.ToString("F1", CultureInfo.InvariantCulture);
        }

        public static bool TextoAFechaHora(string texto, out DateTime fechaHora)
        {
            return DateTime.TryParseExact(texto, ConstantesProtocolo.FormatoFechaHora, CultureInfo.InvariantCulture, DateTimeStyles.None, out fechaHora);
        }

        public static bool TextoATemperatura(string texto, out double temperatura)
        {
            return double.TryParse(texto, NumberStyles.Float, CultureInfo.InvariantCulture, out temperatura);
        }
    }
}
