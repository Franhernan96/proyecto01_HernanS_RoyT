using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace MeteoAula.Pruebas
{
    /// <summary>
    /// Cliente TCP "crudo" para pruebas: envía exactamente los bytes que se le
    /// indican, SIN validar nada. Así se pueden probar mensajes inválidos,
    /// fragmentados o demasiado largos, que el cliente real no permitiría.
    /// </summary>
    public class ClienteCrudo
    {
        // Textos que devuelve LeerLinea cuando no llega una línea normal
        public const string SinRespuesta = "<SIN RESPUESTA>";
        public const string ConexionCerrada = "<CONEXION CERRADA>";

        private TcpClient cliente;
        private NetworkStream flujo;
        private List<byte> pendientes = new List<byte>();
        private byte[] bloque = new byte[512];

        public void Conectar(string ip, int puerto)
        {
            cliente = new TcpClient();
            IAsyncResult intento = cliente.BeginConnect(ip, puerto, null, null);
            if (!intento.AsyncWaitHandle.WaitOne(3000))
            {
                cliente.Close();
                throw new IOException("No se pudo conectar a " + ip + ":" + puerto + " (tiempo agotado)");
            }
            cliente.EndConnect(intento);
            flujo = cliente.GetStream();
        }

        // Envía el texto tal cual (en UTF-8). El LF lo agrega quien llama.
        public void EnviarTexto(string texto)
        {
            byte[] bytes = new UTF8Encoding(false).GetBytes(texto);
            flujo.Write(bytes, 0, bytes.Length);
            flujo.Flush();
        }

        // Lee una línea terminada en LF. Espera como máximo 'esperaMs' milisegundos.
        // Devuelve SinRespuesta si no llegó nada, o ConexionCerrada si el servidor cerró.
        public string LeerLinea(int esperaMs)
        {
            cliente.ReceiveTimeout = esperaMs;

            while (true)
            {
                int posicion = pendientes.IndexOf((byte)'\n');
                if (posicion >= 0)
                {
                    byte[] bytesLinea = pendientes.GetRange(0, posicion).ToArray();
                    pendientes.RemoveRange(0, posicion + 1);
                    return new UTF8Encoding(false).GetString(bytesLinea).TrimEnd('\r');
                }

                int leidos;
                try
                {
                    leidos = flujo.Read(bloque, 0, bloque.Length);
                }
                catch (IOException ex)
                {
                    // Tiempo de espera agotado -> no hay respuesta;
                    // cualquier otro error de red (por ejemplo un reinicio) -> conexión cerrada
                    SocketException socket = ex.InnerException as SocketException;
                    if (socket != null && socket.SocketErrorCode == SocketError.TimedOut)
                    {
                        return SinRespuesta;
                    }
                    return ConexionCerrada;
                }

                if (leidos == 0)
                {
                    return ConexionCerrada;   // el servidor cerró la conexión de forma ordenada
                }
                for (int i = 0; i < leidos; i++)
                {
                    pendientes.Add(bloque[i]);
                }
            }
        }

        // Cierre normal (como cuando un cliente se va sin enviar QUIT)
        public void Cerrar()
        {
            try { if (flujo != null) { flujo.Close(); } } catch (Exception) { }
            try { if (cliente != null) { cliente.Close(); } } catch (Exception) { }
        }

        // Cierre abrupto: envía un RST en vez de un cierre ordenado,
        // igual que cuando el proceso del cliente se mata o se corta la red.
        public void CerrarAbruptamente()
        {
            try
            {
                cliente.Client.LingerState = new LingerOption(true, 0);
                cliente.Close();
            }
            catch (Exception) { }
        }
    }
}
