using System;
using System.Net;
using System.Net.Sockets;
using MeteoAula.Servidor.Servicios;

namespace MeteoAula.Servidor.Transporte
{
    /// <summary>
    /// Escucha conexiones TCP y entrega cada cliente a una SesionCliente.
    /// Atiende un cliente a la vez (suficiente para la nota base): mientras
    /// uno está conectado, los demás esperan en la cola del sistema.
    /// </summary>
    public class ServidorTcp
    {
        private int puerto;
        private ServicioConsultas servicio;
        private TcpListener escuchador;
        private bool ejecutando;

        public ServidorTcp(int puerto, ServicioConsultas servicio)
        {
            this.puerto = puerto;
            this.servicio = servicio;
        }

        /// <summary>
        /// Abre el puerto y atiende clientes hasta que se llame a Detener.
        /// Lanza SocketException si el puerto está ocupado.
        /// </summary>
        public void Iniciar()
        {
            // IPAddress.Any = acepta conexiones desde localhost y desde otros equipos de la red
            escuchador = new TcpListener(IPAddress.Any, puerto);
            escuchador.Start();
            ejecutando = true;

            Console.WriteLine("Servidor MeteoAula escuchando en el puerto {0}. Ctrl+C para detener.", puerto);

            while (ejecutando)
            {
                TcpClient clienteTcp;

                try
                {
                    // Bloquea hasta que un cliente se conecta
                    clienteTcp = escuchador.AcceptTcpClient();
                }
                catch (SocketException)
                {
                    // Al llamar a Detener, AcceptTcpClient lanza esta excepción:
                    // es la forma normal de salir del bucle.
                    if (!ejecutando)
                    {
                        break;
                    }
                    Console.WriteLine("Error al aceptar una conexión; se sigue escuchando.");
                    continue;
                }

                // Un fallo con UN cliente (incluso al crear su sesión) no debe derribar el servidor
                try
                {
                    SesionCliente sesion = new SesionCliente(clienteTcp, servicio);
                    sesion.Atender();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al atender a un cliente: " + ex.Message);
                    try { clienteTcp.Close(); } catch (Exception) { }
                }
            }

            Console.WriteLine("Servidor detenido.");
        }

        public void Detener()
        {
            ejecutando = false;
            if (escuchador != null)
            {
                escuchador.Stop();
            }
        }
    }
}