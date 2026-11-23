using System;
using System.Net.Sockets;
using System.Text;
using MeteoAula.Protocolo;
using MeteoAula.Servidor.Datos;
using MeteoAula.Servidor.Servicios;
using MeteoAula.Servidor.Transporte;

namespace MeteoAula.Servidor
{
    class Programa
    {
        private static ServidorTcp servidor;

        static void Main(string[] args)
        {
            // Para que la consola muestre bien tildes y la ñ
            Console.OutputEncoding = Encoding.UTF8;

            // El puerto se puede pasar como argumento; si no, se usa el 8080
            int puerto = ConstantesProtocolo.PuertoPorDefecto;
            if (args.Length > 0)
            {
                if (!int.TryParse(args[0], out puerto) || puerto < 1 || puerto > 65535)
                {
                    Console.WriteLine("Puerto inválido. Use un número entre 1 y 65535.");
                    return;
                }
            }

            // Se ensamblan las capas de abajo hacia arriba: datos -> lógica -> transporte
            IRepositorioLecturas repositorio = new RepositorioMemoria();
            ServicioConsultas servicio = new ServicioConsultas(repositorio);
            servidor = new ServidorTcp(puerto, servicio);

            Console.CancelKeyPress += AlPresionarCtrlC;

            try
            {
                servidor.Iniciar();
            }
            catch (SocketException ex)
            {
                Console.WriteLine("No se pudo abrir el puerto {0}: {1}", puerto, ex.Message);
                Console.WriteLine("Verifique que no esté en uso por otra aplicación.");
            }
        }

        private static void AlPresionarCtrlC(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;        // evita que el proceso muera de golpe
            servidor.Detener();     // cierra el puerto y termina el bucle
        }
    }
}