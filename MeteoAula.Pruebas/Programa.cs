using System;
using System.Text;

namespace MeteoAula.Pruebas
{
    /// <summary>
    /// Programa de pruebas del Observatorio MeteoAula.
    /// Uso:  MeteoAula.Pruebas.exe [ip] [puerto]     (por defecto 127.0.0.1 y 8080)
    /// </summary>
    class Programa
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            string ip = "127.0.0.1";
            int puerto = 8080;

            if (args.Length > 0)
            {
                ip = args[0];
            }
            if (args.Length > 1)
            {
                if (!int.TryParse(args[1], out puerto) || puerto < 1 || puerto > 65535)
                {
                    Console.WriteLine("Puerto inválido. Use un número entre 1 y 65535.");
                    return;
                }
            }

            bool salir = false;
            while (!salir)
            {
                Console.WriteLine("=== Pruebas MeteoAula (MTP/1) - servidor " + ip + ":" + puerto + " ===");
                Console.WriteLine("1. Ejecutar las pruebas automáticas (28 de 30 casos)");
                Console.WriteLine("2. Iniciar el servidor simulado para CR-07 (puerto 8081)");
                Console.WriteLine("0. Salir");
                Console.Write("Opción: ");
                string opcion = Console.ReadLine();
                Console.WriteLine();

                if (opcion == "1")
                {
                    EjecutarPruebasAutomaticas(ip, puerto);
                }
                else if (opcion == "2")
                {
                    new ServidorSimulado().Ejecutar(8081);
                }
                else if (opcion == "0")
                {
                    salir = true;
                }
            }
        }

        private static void EjecutarPruebasAutomaticas(string ip, int puerto)
        {
            Console.WriteLine("IMPORTANTE: el servidor atiende un solo cliente a la vez.");
            Console.WriteLine("Cierre el cliente de consola antes de continuar.");
            Console.WriteLine("Presione Enter para empezar...");
            Console.ReadLine();

            EjecutorPruebas ejecutor = new EjecutorPruebas(ip, puerto);
            ReportadorResultados reportador = new ReportadorResultados();

            ejecutor.EjecutarTodos(reportador);
            reportador.MostrarResumen(ejecutor.Casos);
            reportador.Guardar(ejecutor.Casos, ip, puerto);
            Console.WriteLine();
        }
    }
}
