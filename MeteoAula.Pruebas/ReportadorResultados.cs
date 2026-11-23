using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MeteoAula.Pruebas
{
    /// <summary>
    /// Muestra los resultados en la consola (para las capturas de pantalla)
    /// y los guarda en un archivo de texto y en un CSV (para la tabla del Logbook).
    /// </summary>
    public class ReportadorResultados
    {
        // Muestra un caso en la consola, con color según el resultado
        public void MostrarCaso(CasoPrueba caso)
        {
            ConsoleColor colorOriginal = Console.ForegroundColor;

            Console.ForegroundColor = ObtenerColor(caso.Resultado);
            Console.WriteLine("[{0}] {1,-38} {2}", caso.Id, caso.Descripcion, caso.Resultado);
            Console.ForegroundColor = colorOriginal;

            Console.WriteLine("        Entrada : " + caso.Entrada);
            Console.WriteLine("        Esperado: " + caso.Esperado);
            Console.WriteLine("        Obtenido: " + caso.Obtenido);
            Console.WriteLine();
        }

        // Resumen final: cuántos casos aprobaron, fallaron o son manuales
        public void MostrarResumen(List<CasoPrueba> casos)
        {
            int aprobados = Contar(casos, "APROBADO");
            int fallidos = Contar(casos, "FALLIDO");
            int manuales = Contar(casos, "MANUAL");

            Console.WriteLine("==============================================");
            Console.WriteLine("RESUMEN: {0} casos | {1} aprobados | {2} fallidos | {3} manuales",
                              casos.Count, aprobados, fallidos, manuales);
            Console.WriteLine("==============================================");
        }

        // Guarda resultados_pruebas.txt y resultados_pruebas.csv junto al ejecutable
        public void Guardar(List<CasoPrueba> casos, string ip, int puerto)
        {
            string carpeta = AppDomain.CurrentDomain.BaseDirectory;
            string rutaTxt = Path.Combine(carpeta, "resultados_pruebas.txt");
            string rutaCsv = Path.Combine(carpeta, "resultados_pruebas.csv");

            // ---- Archivo de texto ----
            StringBuilder texto = new StringBuilder();
            texto.AppendLine("Resultados de las pruebas MeteoAula (MTP/1)");
            texto.AppendLine("Fecha: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
            texto.AppendLine("Servidor probado: " + ip + ":" + puerto);
            texto.AppendLine();
            foreach (CasoPrueba caso in casos)
            {
                texto.AppendLine("[" + caso.Id + "] " + caso.Descripcion + " -> " + caso.Resultado);
                texto.AppendLine("    Entrada : " + caso.Entrada);
                texto.AppendLine("    Esperado: " + caso.Esperado);
                texto.AppendLine("    Obtenido: " + caso.Obtenido);
            }
            texto.AppendLine();
            texto.AppendLine("Aprobados: " + Contar(casos, "APROBADO")
                           + " | Fallidos: " + Contar(casos, "FALLIDO")
                           + " | Manuales: " + Contar(casos, "MANUAL"));
            File.WriteAllText(rutaTxt, texto.ToString(), new UTF8Encoding(true));

            // ---- Archivo CSV (separador ';' para que Excel en español lo abra bien) ----
            StringBuilder csv = new StringBuilder();
            csv.AppendLine("ID;Descripcion;Entrada;Esperado;Obtenido;Resultado");
            foreach (CasoPrueba caso in casos)
            {
                csv.AppendLine(Campo(caso.Id) + ";" + Campo(caso.Descripcion) + ";"
                             + Campo(caso.Entrada) + ";" + Campo(caso.Esperado) + ";"
                             + Campo(caso.Obtenido) + ";" + Campo(caso.Resultado));
            }
            File.WriteAllText(rutaCsv, csv.ToString(), new UTF8Encoding(true));

            Console.WriteLine("Archivos guardados en:");
            Console.WriteLine("  " + rutaTxt);
            Console.WriteLine("  " + rutaCsv);
        }

        private int Contar(List<CasoPrueba> casos, string resultado)
        {
            int cantidad = 0;
            foreach (CasoPrueba caso in casos)
            {
                if (caso.Resultado == resultado)
                {
                    cantidad++;
                }
            }
            return cantidad;
        }

        private ConsoleColor ObtenerColor(string resultado)
        {
            if (resultado == "APROBADO") { return ConsoleColor.Green; }
            if (resultado == "FALLIDO") { return ConsoleColor.Red; }
            return ConsoleColor.Yellow;   // MANUAL
        }

        // Encierra el campo entre comillas y duplica las comillas internas
        private string Campo(string valor)
        {
            return "\"" + valor.Replace("\"", "\"\"") + "\"";
        }
    }
}
