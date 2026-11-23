using System.Collections.Generic;
using System.IO;
using MeteoAula.Protocolo;

namespace MeteoAula.Servidor.Transporte
{
    /// <summary>
    /// Lee mensajes completos desde un flujo TCP.
    ///
    /// TCP es un flujo de bytes SIN límites de mensaje: una lectura puede traer
    /// medio mensaje ("HEL"), un mensaje completo o varios pegados
    /// ("HELLO|1\nHELP\n"). Por eso los bytes se acumulan en 'pendientes' y solo
    /// se entrega una línea cuando aparece el terminador LF ('\n').
    /// Lo que sobra después de un LF se conserva para la siguiente llamada.
    /// </summary>
    public class LectorLineas
    {
        private Stream flujo;
        private byte[] bloque = new byte[512];                      // buffer de cada lectura
        private List<byte> pendientes = new List<byte>();           // bytes recibidos sin procesar

        public LectorLineas(Stream flujo)
        {
            this.flujo = flujo;
        }

        /// <summary>
        /// Devuelve la siguiente línea (sin LF ni CR final), o null si el
        /// cliente cerró la conexión. Lanza ExcepcionProtocolo (403) si el
        /// mensaje supera el máximo. Si la red falla lanza IOException,
        /// que atrapa SesionCliente.
        /// </summary>
        public string LeerLinea()
        {
            while (true)
            {
                // 1. ¿Ya hay un mensaje completo entre los bytes acumulados?
                int posicionTerminador = pendientes.IndexOf((byte)ConstantesProtocolo.TerminadorMensaje);

                if (posicionTerminador >= 0)
                {
                    // El tamaño del mensaje cuenta el LF: 255 caracteres + LF = 256 (válido)
                    int bytesDelMensaje = posicionTerminador + 1;
                    if (bytesDelMensaje > ConstantesProtocolo.MaxBytesMensaje)
                    {
                        throw new ExcepcionProtocolo(CodigosRespuesta.MensajeMuyLargo,
                            "El mensaje supera los " + ConstantesProtocolo.MaxBytesMensaje + " bytes permitidos");
                    }

                    // Se extrae el mensaje y se descarta del acumulador
                    // (lo que quede son bytes del siguiente mensaje)
                    byte[] bytesLinea = pendientes.GetRange(0, posicionTerminador).ToArray();
                    pendientes.RemoveRange(0, bytesDelMensaje);

                    // Se tolera CR+LF: si el mensaje termina en CR, se descarta
                    int largo = bytesLinea.Length;
                    if (largo > 0 && bytesLinea[largo - 1] == (byte)'\r')
                    {
                        largo--;
                    }

                    return ConstantesProtocolo.Codificacion.GetString(bytesLinea, 0, largo);
                }

                // 2. Todavía no hay LF. Si ya se acumularon 256 bytes sin encontrarlo,
                //    el mensaje es demasiado largo (ni con el LF cabría). Se corta aquí
                //    para que un cliente no pueda llenar la memoria enviando sin parar.
                if (pendientes.Count >= ConstantesProtocolo.MaxBytesMensaje)
                {
                    throw new ExcepcionProtocolo(CodigosRespuesta.MensajeMuyLargo,
                        "El mensaje supera los " + ConstantesProtocolo.MaxBytesMensaje + " bytes permitidos");
                }

                // 3. Faltan datos: se lee de la red (esta llamada bloquea hasta que llegue algo)
                int leidos = flujo.Read(bloque, 0, bloque.Length);

                // Read devuelve 0 cuando el otro extremo cerró la conexión de forma ordenada
                if (leidos == 0)
                {
                    return null;
                }

                for (int i = 0; i < leidos; i++)
                {
                    pendientes.Add(bloque[i]);
                }
            }
        }
    }
}