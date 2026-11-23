using System;
using System.IO;
using System.Net.Sockets;
using MeteoAula.Protocolo;

namespace MeteoAula.Cliente
{
    /// <summary>
    /// Capa de transporte del cliente: abre la conexión TCP, envía líneas
    /// terminadas en LF y lee líneas del servidor.
    /// </summary>
    public class ConexionCliente
    {
        private TcpClient cliente;
        private StreamReader lector;
        private StreamWriter escritor;

        public bool Conectado { get; private set; }

        public void Conectar(string ip, int puerto)
        {
            Desconectar();   // por si quedaba una conexión anterior

            TcpClient nuevo = new TcpClient();
            try
            {
                // Se espera máximo 5 segundos: con una IP equivocada, Connect
                // normal se queda bloqueado más de 20 segundos.
                IAsyncResult intento = nuevo.BeginConnect(ip, puerto, null, null);
                if (!intento.AsyncWaitHandle.WaitOne(5000))
                {
                    throw new IOException("Tiempo de espera agotado al conectar");
                }
                nuevo.EndConnect(intento);   // lanza SocketException si el servidor rechazó la conexión
            }
            catch (Exception)
            {
                nuevo.Close();
                throw;   // se vuelve a lanzar para que la consola muestre el error
            }

            // Si el servidor no responde en 10 s, ReadLine lanza IOException
            nuevo.ReceiveTimeout = 10000;

            NetworkStream flujo = nuevo.GetStream();
            lector = new StreamReader(flujo, ConstantesProtocolo.Codificacion);
            escritor = new StreamWriter(flujo, ConstantesProtocolo.Codificacion);
            escritor.AutoFlush = true;

            cliente = nuevo;
            Conectado = true;
        }

        // Envía una línea terminada en LF. El LF es lo que permite al servidor
        // saber dónde termina el mensaje.
        public void EnviarLinea(string linea)
        {
            if (!Conectado)
            {
                throw new IOException("No hay conexión activa");
            }
            escritor.Write(linea + ConstantesProtocolo.TerminadorMensaje);
        }

        // Lee una línea completa (ReadLine espera hasta recibir el LF).
        public string LeerLinea()
        {
            if (!Conectado)
            {
                throw new IOException("No hay conexión activa");
            }

            string linea = lector.ReadLine();

            // ReadLine devuelve null cuando el servidor cerró la conexión:
            // así se detecta una desconexión.
            if (linea == null)
            {
                Desconectar();
                throw new IOException("El servidor cerró la conexión");
            }
            return linea;
        }

        public void Desconectar()
        {
            Conectado = false;

            // Cada cierre va en su propio try-catch: si la red ya se cayó,
            // cerrar puede lanzar excepciones que no nos interesan.
            try { if (escritor != null) { escritor.Close(); } } catch (Exception) { }
            try { if (lector != null) { lector.Close(); } } catch (Exception) { }
            try { if (cliente != null) { cliente.Close(); } } catch (Exception) { }

            escritor = null;
            lector = null;
            cliente = null;
        }
    }
}