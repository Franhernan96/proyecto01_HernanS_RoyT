using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeteoAula.Protocolo
{
    /// <summary>
    /// Convierte una línea de texto en un objeto Comando.
    /// Valida solo lo más básico: que la línea no esté vacía y que el comando exista.
    /// La cantidad y el formato de los parámetros los valida el intérprete.
    /// </summary>
    public class AnalizadorComandos
    {
        public Comando Analizar(string linea)
        {
            // Línea vacía -> 402
            if (linea == null || linea.Trim().Length == 0)
            {
                throw new ExcepcionProtocolo(CodigosRespuesta.FormatoInvalido, "La línea está vacía");
            }

            // Se separa por '|'. Se limpian espacios sobrantes para ser tolerantes.
            string[] partes = linea.Trim().Split(ConstantesProtocolo.SeparadorCampos);

            // Los comandos no distinguen mayúsculas de minúsculas
            string nombre = partes[0].Trim().ToUpperInvariant();

            if (nombre.Length == 0)
            {
                throw new ExcepcionProtocolo(CodigosRespuesta.FormatoInvalido, "Falta el nombre del comando");
            }

            // Comando que no existe -> 400
            if (!EsComandoConocido(nombre))
            {
                throw new ExcepcionProtocolo(CodigosRespuesta.ComandoDesconocido, "El comando no existe");
            }

            List<string> parametros = new List<string>();
            for (int i = 1; i < partes.Length; i++)
            {
                parametros.Add(partes[i].Trim());
            }

            return new Comando(nombre, parametros);
        }

        private bool EsComandoConocido(string nombre)
        {
            return nombre == NombresComandos.Hello
                || nombre == NombresComandos.Help
                || nombre == NombresComandos.List
                || nombre == NombresComandos.Current
                || nombre == NombresComandos.History
                || nombre == NombresComandos.Summary
                || nombre == NombresComandos.Quit;
        }
    }
}
