using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using MeteoAula.Protocolo;
using MeteoAula.Servidor.Interprete;
using MeteoAula.Servidor.Servicios;

namespace MeteoAula.Servidor.Transporte
{
    /// <summary>
    /// Atiende a UN cliente conectado: lee líneas, las entrega al intérprete
    /// y escribe las respuestas. También registra en consola las conexiones
    /// y los comandos recibidos.
    /// </summary>
    public class SesionCliente
    {
        private TcpClient cliente;
        private NetworkStream flujo;
        private InterpreteProtocolo interprete;
        private string direccionCliente;

        public SesionCliente(TcpClient cliente, ServicioConsultas servicio)
        {
            this.cliente = cliente;
            this.interprete = new InterpreteProtocolo(servicio);
            // Se guarda ahora porque después de cerrar el socket ya no se puede consultar
            this.direccionCliente = cliente.Client.RemoteEndPoint.ToString();
        }

        public void Atender()
        {
            Registrar("Cliente conectado");

            try
            {
                cliente.ReceiveTimeout = 300000;   // 5 minutos sin actividad: se cierra la sesión
                flujo = cliente.GetStream();
                LectorLineas lector = new LectorLineas(flujo);

                // Se repite hasta que el intérprete marque la sesión como Cerrado (QUIT)
                while (!interprete.SesionTerminada)
                {
                    string linea;

                    try
                    {
                        linea = lector.LeerLinea();
                    }
                    catch (ExcepcionProtocolo ex)
                    {
                        // Mensaje demasiado largo (403): según el contrato se
                        // avisa al cliente y se cierra la conexión.
                        Registrar("Mensaje rechazado: " + ex.Message);
                        EnviarLinea(InterpreteProtocolo.CrearLineaError(ex.Codigo, ex.Message));
                        break;
                    }

                    // null = el cliente cerró la conexión sin enviar QUIT
                    if (linea == null)
                    {
                        Registrar("El cliente cerró la conexión sin enviar QUIT");
                        break;
                    }

                    Registrar("Comando recibido: " + linea);

                    List<string> respuestas = interprete.Procesar(linea);
                    foreach (string respuesta in respuestas)
                    {
                        EnviarLinea(respuesta);
                    }
                }
            }
            // ---------- DESCONEXIONES ABRUPTAS ----------
            // Si el cliente se cae (cable desconectado, proceso terminado, reinicio
            // de la conexión), Read o Write lanzan IOException. Se atrapa aquí para
            // que el error de UN cliente nunca derribe al servidor.
            catch (IOException ex)
            {
                Registrar("Conexión interrumpida: " + ex.Message);
            }
            catch (ObjectDisposedException)
            {
                Registrar("La conexión ya estaba cerrada");
            }
            catch (Exception ex)
            {
                // Error no previsto: se informa al cliente (500) y se cierra su sesión.
                Registrar("Error inesperado: " + ex.Message);
                IntentarEnviarError500();
            }
            finally
            {
                // Se ejecuta SIEMPRE: termine bien o con error, el socket se libera
                Cerrar();
                Registrar("Cliente desconectado");
            }
        }

        // Escribe una línea terminada en LF. Así el cliente puede delimitar el mensaje.
        private void EnviarLinea(string linea)
        {
            byte[] bytes = ConstantesProtocolo.Codificacion.GetBytes(
                linea + ConstantesProtocolo.TerminadorMensaje);

            flujo.Write(bytes, 0, bytes.Length);
            flujo.Flush();

            Registrar("Respuesta enviada: " + linea);
        }

        private void IntentarEnviarError500()
        {
            try
            {
                EnviarLinea(InterpreteProtocolo.CrearLineaError(
                    CodigosRespuesta.ErrorInterno, "Error interno del servidor"));
            }
            catch (Exception)
            {
                // Si tampoco se puede escribir, la conexión ya no sirve: no hay nada más que hacer
            }
        }

        private void Cerrar()
        {
            try
            {
                cliente.Close();
            }
            catch (Exception)
            {
                // Ya estaba cerrado; no importa
            }
        }

        private void Registrar(string mensaje)
        {
            Console.WriteLine("[{0}] [{1}] {2}", DateTime.Now.ToString("HH:mm:ss"),
                              direccionCliente, mensaje);
        }
    }
}
