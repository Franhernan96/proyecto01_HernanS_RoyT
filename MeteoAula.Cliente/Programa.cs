using System;
using System.Globalization;
using System.Text;
using MeteoAula.Protocolo;

namespace MeteoAula.Cliente
{
    class Programa
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            // Valores por defecto; se pueden pasar como argumentos: MeteoAula.Cliente.exe 192.168.1.10 8080
            string ip = "127.0.0.1";
            int puerto = ConstantesProtocolo.PuertoPorDefecto;

            if (args.Length > 0)
            {
                ip = args[0];
            }
            if (args.Length > 1)
            {
                if (!int.TryParse(args[1], NumberStyles.None, CultureInfo.InvariantCulture, out puerto)
                    || puerto < 1 || puerto > 65535)
                {
                    Console.WriteLine("Puerto inválido. Use un número entre 1 y 65535.");
                    return;
                }
            }

            ConsolaCliente consola = new ConsolaCliente(ip, puerto);
            consola.Ejecutar();
        }
    }
}