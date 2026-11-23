using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using MeteoAula.Protocolo;

namespace MeteoAula.Cliente
{
    /// <summary>
    /// Interpreta y valida las líneas del servidor. Si algo no cumple el
    /// protocolo, lanza ExcepcionProtocolo en vez de mostrarlo como si fuera válido.
    /// </summary>
    public class AnalizadorRespuestas
    {
        // Convierte "<codigo>|<ETIQUETA>|campo1|campo2..." en un objeto Respuesta
        public Respuesta Analizar(string linea)
        {
            if (linea == null || linea.Trim().Length == 0)
            {
                throw new ExcepcionProtocolo(CodigosRespuesta.FormatoInvalido, "El servidor envió una respuesta vacía");
            }

            string[] partes = linea.Split(ConstantesProtocolo.SeparadorCampos);

            // El primer campo debe ser un código de exactamente 3 dígitos
            int codigo;
            if (!int.TryParse(partes[0], NumberStyles.None, CultureInfo.InvariantCulture, out codigo) || partes[0].Length != 3)
            {
                throw new ExcepcionProtocolo(CodigosRespuesta.FormatoInvalido, "La respuesta no empieza con un código de 3 dígitos");
            }

            string etiqueta = "";
            int primerCampo = 1;

            // Las filas de datos (211 y 212) no llevan etiqueta; el resto sí
            if (TieneEtiqueta(codigo))
            {
                if (partes.Length < 2)
                {
                    throw new ExcepcionProtocolo(CodigosRespuesta.FormatoInvalido, "A la respuesta le falta la etiqueta");
                }
                etiqueta = partes[1];
                primerCampo = 2;
            }

            List<string> campos = new List<string>();
            for (int i = primerCampo; i < partes.Length; i++)
            {
                campos.Add(partes[i]);
            }

            return new Respuesta(codigo, etiqueta, campos, linea);
        }

        // Extrae el número de filas anunciado en "210|COUNT|n"
        public int ConvertirACantidad(Respuesta respuesta)
        {
            int cantidad;
            if (respuesta.Codigo != CodigosRespuesta.Conteo
                || respuesta.Campos.Count != 1
                || !int.TryParse(respuesta.Campos[0], NumberStyles.None,
                                 CultureInfo.InvariantCulture, out cantidad))
            {
                throw new ExcepcionProtocolo(CodigosRespuesta.FormatoInvalido, "Se esperaba el inicio de una lista (210|COUNT|n)");
            }
            return cantidad;
        }

        // Fila 211: codigo|nombre|ciudad
        public Estacion ConvertirAEstacion(Respuesta respuesta)
        {
            if (respuesta.Codigo != CodigosRespuesta.FilaEstacion || respuesta.Campos.Count != 3)
            {
                throw new ExcepcionProtocolo(CodigosRespuesta.FormatoInvalido, "Fila de estación inválida");
            }
            return new Estacion(respuesta.Campos[0], respuesta.Campos[1], respuesta.Campos[2]);
        }

        // Respuesta 203 (lectura actual) o fila 212 (historial): fecha|temp|hum|estado
        public Lectura ConvertirALectura(Respuesta respuesta)
        {
            bool codigoValido = respuesta.Codigo == CodigosRespuesta.LecturaActual || respuesta.Codigo == CodigosRespuesta.FilaLectura;

            if (!codigoValido || respuesta.Campos.Count != 4)
            {
                throw new ExcepcionProtocolo(CodigosRespuesta.FormatoInvalido, "Lectura con formato inválido");
            }

            DateTime fechaHora;
            double temperatura;
            int humedad;

            if (!ConvertidorDatos.TextoAFechaHora(respuesta.Campos[0], out fechaHora)
                || !ConvertidorDatos.TextoATemperatura(respuesta.Campos[1], out temperatura)
                || !int.TryParse(respuesta.Campos[2], NumberStyles.None,
                                 CultureInfo.InvariantCulture, out humedad))
            {
                throw new ExcepcionProtocolo(CodigosRespuesta.FormatoInvalido, "La lectura contiene fecha, temperatura o humedad inválidas");
            }

            return new Lectura(fechaHora, temperatura, humedad, respuesta.Campos[3]);
        }

        // Respuesta 204: promedio|minimo|maximo|cantidad
        public ResumenTemperatura ConvertirAResumen(Respuesta respuesta)
        {
            if (respuesta.Codigo != CodigosRespuesta.Resumen || respuesta.Campos.Count != 4)
            {
                throw new ExcepcionProtocolo(CodigosRespuesta.FormatoInvalido, "Resumen con formato inválido");
            }

            double promedio;
            double minimo;
            double maximo;
            int cantidad;

            if (!ConvertidorDatos.TextoATemperatura(respuesta.Campos[0], out promedio)
                || !ConvertidorDatos.TextoATemperatura(respuesta.Campos[1], out minimo)
                || !ConvertidorDatos.TextoATemperatura(respuesta.Campos[2], out maximo)
                || !int.TryParse(respuesta.Campos[3], NumberStyles.None, CultureInfo.InvariantCulture, out cantidad))
            {
                throw new ExcepcionProtocolo(CodigosRespuesta.FormatoInvalido, "El resumen contiene valores inválidos");
            }

            return new ResumenTemperatura(promedio, minimo, maximo, cantidad);
        }

        private bool TieneEtiqueta(int codigo)
        {
            return codigo != CodigosRespuesta.FilaEstacion
                && codigo != CodigosRespuesta.FilaLectura;
        }
    }
}
