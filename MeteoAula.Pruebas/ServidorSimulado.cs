using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MeteoAula.Pruebas
{
    /// <summary>
    /// Servidor falso para el caso CR-07. Responde HELLO bien, pero a LIST
    /// le anuncia "210|COUNT|3" y envía solo 2 filas antes del "219|END".
    /// Sirve para comprobar que el cliente real NO muestra como válida una
    /// respuesta incoherente.
    /// </summary>
    public class ServidorSimulado
    {
        public void Ejecutar(int puerto)
        {
            TcpListener escuchador = new TcpListener(IPAddress.Any, puerto);
            escuchador.Start();

            Console.WriteLine("Servidor simulado escuchando en el puerto " + puerto + ".");
            Console.WriteLine("Conecte el cliente de consola:  MeteoAula.Cliente.exe 127.0.0.1 " + puerto);
            Console.WriteLine("Elija la opción 3 (LIST) y observe el resultado. Cierre con Ctrl+C.");
            Console.WriteLine();

            while (true)
            {
                TcpClient cliente = escuchador.AcceptTcpClient();
                Console.WriteLine("Cliente conectado.");
                try
                {
                    Atender(cliente);
                }
                catch (IOException)
                {
                    Console.WriteLine("El cliente se desconectó.");
                }
                finally
                {
                    cliente.Close();
                }
            }
        }

        private void Atender(TcpClient cliente)
        {
            NetworkStream flujo = cliente.GetStream();
            UTF8Encoding codificacion = new UTF8Encoding(false);
            StreamReader lector = new StreamReader(flujo, codificacion);
            StreamWriter escritor = new StreamWriter(flujo, codificacion);
            escritor.NewLine = "\n";
            escritor.AutoFlush = true;

            while (true)
            {
                string linea = lector.ReadLine();
                if (linea == null)
                {
                    return;   // el cliente cerró la conexión
                }
                Console.WriteLine("Recibido: " + linea);

                string comando = linea.Split('|')[0].Trim().ToUpperInvariant();

                if (comando == "HELLO")
                {
                    Enviar(escritor, "200|WELCOME|1");
                }
                else if (comando == "LIST")
                {
                    // INCOHERENTE: se anuncian 3 filas pero solo se envían 2
                    Enviar(escritor, "210|COUNT|3");
                    Enviar(escritor, "211|UIO|Mariscal Sucre|Quito");
                    Enviar(escritor, "211|GYE|José Joaquín de Olmedo|Guayaquil");
                    Enviar(escritor, "219|END");
                }
                else if (comando == "QUIT")
                {
                    Enviar(escritor, "201|BYE");
                    return;
                }
                else
                {
                    Enviar(escritor, "500|INTERNAL_ERROR|Comando no simulado");
                }
            }
        }

        private void Enviar(StreamWriter escritor, string linea)
        {
            escritor.WriteLine(linea);
            Console.WriteLine("Enviado : " + linea);
        }
    }
}
