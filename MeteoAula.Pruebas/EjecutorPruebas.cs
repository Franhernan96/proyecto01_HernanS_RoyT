using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace MeteoAula.Pruebas
{
    /// <summary>
    /// Ejecuta los 30 casos de prueba del Logbook contra el servidor real.
    /// Cada caso abre su propia conexión y la cierra al terminar, porque el
    /// servidor atiende un solo cliente a la vez.
    /// </summary>
    public class EjecutorPruebas
    {
        private const int EsperaMs = 3000;
        private const string AyudaConectado = "202|HELP|HELLO;HELP;QUIT";
        private const string Bienvenida = "200|WELCOME|1";

        private string ip;
        private int puerto;
        private List<CasoPrueba> casos = new List<CasoPrueba>();

        public List<CasoPrueba> Casos
        {
            get { return casos; }
        }

        public EjecutorPruebas(string ip, int puerto)
        {
            this.ip = ip;
            this.puerto = puerto;
            CrearCasos();
        }

        // ---------------- DEFINICIÓN DE LOS 30 CASOS ----------------

        private void CrearCasos()
        {
            // Casos positivos
            casos.Add(new CasoPrueba("CP-01", "Identificación correcta", "Sesión nueva: HELLO|1", "200|WELCOME|1"));
            casos.Add(new CasoPrueba("CP-02", "Listar estaciones", "LIST", "210|COUNT|5, 5 filas 211, 219|END"));
            casos.Add(new CasoPrueba("CP-03", "Lectura más reciente", "CURRENT|UIO", "203|CURRENT|2026-10-01T19:00|17.2|68|NUBLADO"));
            casos.Add(new CasoPrueba("CP-04", "Historial de 2 registros", "HISTORY|UIO|2", "210|COUNT|2, filas de las 18:00 y 19:00, 219|END"));
            casos.Add(new CasoPrueba("CP-05", "Resumen de temperatura", "SUMMARY|UIO", "204|SUMMARY|19.1|16.8|21.4|12"));
            casos.Add(new CasoPrueba("CP-06", "Ayuda antes de HELLO", "Sesión nueva: HELP", AyudaConectado));
            casos.Add(new CasoPrueba("CP-07", "Cierre limpio", "QUIT", "201|BYE y el servidor cierra el socket"));

            // Casos negativos
            casos.Add(new CasoPrueba("CN-01", "Consulta antes de HELLO", "Sesión nueva: LIST", "406|INVALID_STATE|...; la sesión sigue abierta"));
            casos.Add(new CasoPrueba("CN-02", "Comando desconocido", "FOO", "400|UNKNOWN_COMMAND|..."));
            casos.Add(new CasoPrueba("CN-03", "Parámetro faltante", "CURRENT", "401|MISSING_PARAMS|..."));
            casos.Add(new CasoPrueba("CN-04", "N no numérico", "HISTORY|UIO|abc", "402|INVALID_FORMAT|..."));
            casos.Add(new CasoPrueba("CN-05", "Estación inexistente", "CURRENT|XXX", "404|STATION_NOT_FOUND|..."));
            casos.Add(new CasoPrueba("CN-06", "Versión no soportada", "Sesión nueva: HELLO|9", "407|UNSUPPORTED_VERSION|...; sigue en Conectado"));
            casos.Add(new CasoPrueba("CN-07", "HELLO repetido", "HELLO|1", "406|INVALID_STATE|..."));
            casos.Add(new CasoPrueba("CN-08", "N fuera de rango", "HISTORY|UIO|99", "405|OUT_OF_RANGE|..."));
            casos.Add(new CasoPrueba("CN-09", "Parámetros de más", "LIST|x", "402|INVALID_FORMAT|..."));
            casos.Add(new CasoPrueba("CN-10", "Línea vacía", "Solo LF", "402|INVALID_FORMAT|..."));

            // Casos límite
            casos.Add(new CasoPrueba("CL-01", "N mínimo", "HISTORY|UIO|1", "210|COUNT|1, 1 fila, 219|END"));
            casos.Add(new CasoPrueba("CL-02", "N máximo", "HISTORY|UIO|10", "210|COUNT|10, 10 filas, 219|END"));
            casos.Add(new CasoPrueba("CL-03", "N bajo el mínimo", "HISTORY|UIO|0", "405|OUT_OF_RANGE|..."));
            casos.Add(new CasoPrueba("CL-04", "N sobre el máximo", "HISTORY|UIO|11", "405|OUT_OF_RANGE|..."));
            casos.Add(new CasoPrueba("CL-05", "Mensaje de 256 bytes", "255 caracteres + LF", "400|UNKNOWN_COMMAND|...; la conexión sigue abierta"));
            casos.Add(new CasoPrueba("CL-06", "Mensaje de 257 bytes", "256 caracteres + LF", "403|MESSAGE_TOO_LONG|... y el servidor cierra la conexión"));

            // Casos de robustez
            casos.Add(new CasoPrueba("CR-01", "Mensaje fragmentado", "HEL, esperar 1 s, LO|1\\n", "Una sola respuesta 200|WELCOME|1"));
            casos.Add(new CasoPrueba("CR-02", "Varios mensajes en una lectura", "HELLO|1\\nHELP\\n en un solo envío", "Dos respuestas en orden: 200 y 202"));
            casos.Add(new CasoPrueba("CR-03", "Terminador CR+LF", "HELLO|1\\r\\n", "200|WELCOME|1"));
            casos.Add(new CasoPrueba("CR-04", "Mayúsculas y minúsculas", "hello|1 y luego current|uio", "200 y luego 203"));
            casos.Add(new CasoPrueba("CR-05", "Desconexión abrupta del cliente", "Cierre abrupto (RST) sin QUIT", "El servidor sigue activo y acepta otra conexión"));
            casos.Add(new CasoPrueba("CR-06", "Servidor caído durante la sesión", "Cerrar el servidor con el cliente conectado", "El cliente detecta la pérdida y permite reconectar"));
            casos.Add(new CasoPrueba("CR-07", "Respuesta multi-registro incoherente", "Servidor simulado: COUNT|3 con solo 2 filas", "El cliente reporta error de protocolo"));
        }

        // ---------------- EJECUCIÓN ----------------

        // Ejecuta todos los casos y muestra cada resultado apenas termina.
        public void EjecutarTodos(ReportadorResultados reportador)
        {
            foreach (CasoPrueba caso in casos)
            {
                EjecutarCaso(caso);
                reportador.MostrarCaso(caso);
            }
        }

        private void EjecutarCaso(CasoPrueba caso)
        {
            try
            {
                switch (caso.Id)
                {
                    case "CP-01": CP01(caso); break;
                    case "CP-02": CP02(caso); break;
                    case "CP-03": ProbarRespuestaExacta(caso, "CURRENT|UIO", "203|CURRENT|2026-10-01T19:00|17.2|68|NUBLADO"); break;
                    case "CP-04": CP04(caso); break;
                    case "CP-05": ProbarRespuestaExacta(caso, "SUMMARY|UIO", "204|SUMMARY|19.1|16.8|21.4|12"); break;
                    case "CP-06": CP06(caso); break;
                    case "CP-07": CP07(caso); break;

                    case "CN-01": ProbarErrorYContinuar(caso, "LIST", "406|INVALID_STATE|", AyudaConectado); break;
                    case "CN-02": ProbarError(caso, "FOO", "400|UNKNOWN_COMMAND|"); break;
                    case "CN-03": ProbarError(caso, "CURRENT", "401|MISSING_PARAMS|"); break;
                    case "CN-04": ProbarError(caso, "HISTORY|UIO|abc", "402|INVALID_FORMAT|"); break;
                    case "CN-05": ProbarError(caso, "CURRENT|XXX", "404|STATION_NOT_FOUND|"); break;
                    case "CN-06": CN06(caso); break;
                    case "CN-07": ProbarError(caso, "HELLO|1", "406|INVALID_STATE|"); break;
                    case "CN-08": ProbarError(caso, "HISTORY|UIO|99", "405|OUT_OF_RANGE|"); break;
                    case "CN-09": ProbarError(caso, "LIST|x", "402|INVALID_FORMAT|"); break;
                    case "CN-10": ProbarError(caso, "", "402|INVALID_FORMAT|"); break;

                    case "CL-01": ProbarHistorial(caso, 1); break;
                    case "CL-02": ProbarHistorial(caso, 10); break;
                    case "CL-03": ProbarError(caso, "HISTORY|UIO|0", "405|OUT_OF_RANGE|"); break;
                    case "CL-04": ProbarError(caso, "HISTORY|UIO|11", "405|OUT_OF_RANGE|"); break;
                    case "CL-05": CL05(caso); break;
                    case "CL-06": CL06(caso); break;

                    case "CR-01": CR01(caso); break;
                    case "CR-02": CR02(caso); break;
                    case "CR-03": CR03(caso); break;
                    case "CR-04": CR04(caso); break;
                    case "CR-05": CR05(caso); break;

                    case "CR-06":
                        caso.Resultado = "MANUAL";
                        caso.Obtenido = "Se prueba a mano: cerrar el servidor (Ctrl+C) con el cliente conectado y enviar un comando";
                        break;
                    case "CR-07":
                        caso.Resultado = "MANUAL";
                        caso.Obtenido = "Se prueba a mano: opción 2 de este programa + cliente de consola en el puerto 8081";
                        break;
                }
            }
            catch (Exception ex)
            {
                // Si algo falla (por ejemplo no hay conexión) el caso se marca como fallido
                caso.Resultado = "FALLIDO";
                caso.Obtenido = "Excepción: " + ex.Message;
            }
        }

        // ---------------- AYUDAS ----------------

        private ClienteCrudo AbrirSesionNueva()
        {
            ClienteCrudo cliente = new ClienteCrudo();
            cliente.Conectar(ip, puerto);
            return cliente;
        }

        // Abre una conexión y envía HELLO|1 para dejarla en el estado Consultas
        private ClienteCrudo AbrirSesionIdentificada()
        {
            ClienteCrudo cliente = AbrirSesionNueva();
            cliente.EnviarTexto("HELLO|1\n");
            string respuesta = cliente.LeerLinea(EsperaMs);
            if (respuesta != Bienvenida)
            {
                cliente.Cerrar();
                throw new IOException("No se pudo identificar la sesión. Respuesta: " + respuesta);
            }
            return cliente;
        }

        private List<string> LeerLineas(ClienteCrudo cliente, int cantidad)
        {
            List<string> lineas = new List<string>();
            for (int i = 0; i < cantidad; i++)
            {
                lineas.Add(cliente.LeerLinea(EsperaMs));
            }
            return lineas;
        }

        private bool Comienza(string texto, string prefijo)
        {
            return texto.StartsWith(prefijo, StringComparison.Ordinal);
        }

        // Para mostrar listas largas de forma compacta
        private string Resumir(List<string> lineas)
        {
            if (lineas.Count <= 3)
            {
                return string.Join(" // ", lineas.ToArray());
            }
            return lineas[0] + " // (" + (lineas.Count - 2) + " filas) // " + lineas[lineas.Count - 1];
        }

        private void Finalizar(CasoPrueba caso, bool correcto)
        {
            caso.Resultado = correcto ? "APROBADO" : "FALLIDO";
        }

        // ---------------- PATRONES REUTILIZABLES ----------------

        // Sesión identificada: envía un comando y compara la respuesta completa
        private void ProbarRespuestaExacta(CasoPrueba caso, string envio, string esperada)
        {
            ClienteCrudo cliente = AbrirSesionIdentificada();
            try
            {
                cliente.EnviarTexto(envio + "\n");
                string respuesta = cliente.LeerLinea(EsperaMs);
                caso.Obtenido = respuesta;
                Finalizar(caso, respuesta == esperada);
            }
            finally { cliente.Cerrar(); }
        }

        // Sesión identificada: la respuesta debe empezar con "codigo|ETIQUETA|"
        private void ProbarError(CasoPrueba caso, string envio, string prefijo)
        {
            ClienteCrudo cliente = AbrirSesionIdentificada();
            try
            {
                cliente.EnviarTexto(envio + "\n");
                string respuesta = cliente.LeerLinea(EsperaMs);
                caso.Obtenido = respuesta;
                Finalizar(caso, Comienza(respuesta, prefijo));
            }
            finally { cliente.Cerrar(); }
        }

        // Sesión NUEVA: provoca un error y comprueba que la sesión sigue abierta
        // enviando HELP (en estado Conectado debe dar la ayuda de 3 comandos).
        private void ProbarErrorYContinuar(CasoPrueba caso, string envio, string prefijo, string ayudaEsperada)
        {
            ClienteCrudo cliente = AbrirSesionNueva();
            try
            {
                cliente.EnviarTexto(envio + "\n");
                string respuesta = cliente.LeerLinea(EsperaMs);

                cliente.EnviarTexto("HELP\n");
                string ayuda = cliente.LeerLinea(EsperaMs);

                caso.Obtenido = respuesta + " // luego HELP -> " + ayuda;
                Finalizar(caso, Comienza(respuesta, prefijo) && ayuda == ayudaEsperada);
            }
            finally { cliente.Cerrar(); }
        }

        // HISTORY|UIO|n: COUNT con n, n filas 212 y END. Devuelve las líneas leídas.
        private List<string> LeerHistorial(ClienteCrudo cliente, int n, out bool correcto)
        {
            cliente.EnviarTexto("HISTORY|UIO|" + n + "\n");
            List<string> lineas = LeerLineas(cliente, n + 2);

            correcto = lineas[0] == "210|COUNT|" + n && lineas[n + 1] == "219|END";
            for (int i = 1; i <= n; i++)
            {
                if (!Comienza(lineas[i], "212|"))
                {
                    correcto = false;
                }
            }
            return lineas;
        }

        private void ProbarHistorial(CasoPrueba caso, int n)
        {
            ClienteCrudo cliente = AbrirSesionIdentificada();
            try
            {
                bool correcto;
                List<string> lineas = LeerHistorial(cliente, n, out correcto);
                caso.Obtenido = Resumir(lineas);
                Finalizar(caso, correcto);
            }
            finally { cliente.Cerrar(); }
        }

        // ---------------- CASOS CON LÓGICA PROPIA ----------------

        private void CP01(CasoPrueba caso)
        {
            ClienteCrudo cliente = AbrirSesionNueva();
            try
            {
                cliente.EnviarTexto("HELLO|1\n");
                string respuesta = cliente.LeerLinea(EsperaMs);
                caso.Obtenido = respuesta;
                Finalizar(caso, respuesta == Bienvenida);
            }
            finally { cliente.Cerrar(); }
        }

        private void CP02(CasoPrueba caso)
        {
            ClienteCrudo cliente = AbrirSesionIdentificada();
            try
            {
                cliente.EnviarTexto("LIST\n");
                List<string> lineas = LeerLineas(cliente, 7);

                bool correcto = lineas[0] == "210|COUNT|5" && lineas[6] == "219|END";
                for (int i = 1; i <= 5; i++)
                {
                    if (!Comienza(lineas[i], "211|"))
                    {
                        correcto = false;
                    }
                }
                caso.Obtenido = Resumir(lineas);
                Finalizar(caso, correcto);
            }
            finally { cliente.Cerrar(); }
        }

        // El historial debe venir en orden cronológico: primero las 18:00 y luego las 19:00
        private void CP04(CasoPrueba caso)
        {
            ClienteCrudo cliente = AbrirSesionIdentificada();
            try
            {
                bool correcto;
                List<string> lineas = LeerHistorial(cliente, 2, out correcto);

                bool ordenCorrecto = Comienza(lineas[1], "212|2026-10-01T18:00|")
                                  && Comienza(lineas[2], "212|2026-10-01T19:00|");
                caso.Obtenido = string.Join(" // ", lineas.ToArray());
                Finalizar(caso, correcto && ordenCorrecto);
            }
            finally { cliente.Cerrar(); }
        }

        private void CP06(CasoPrueba caso)
        {
            ClienteCrudo cliente = AbrirSesionNueva();
            try
            {
                cliente.EnviarTexto("HELP\n");
                string respuesta = cliente.LeerLinea(EsperaMs);
                caso.Obtenido = respuesta;
                Finalizar(caso, respuesta == AyudaConectado);
            }
            finally { cliente.Cerrar(); }
        }

        // Tras QUIT el servidor responde 201 y cierra: la siguiente lectura debe indicar cierre
        private void CP07(CasoPrueba caso)
        {
            ClienteCrudo cliente = AbrirSesionIdentificada();
            try
            {
                cliente.EnviarTexto("QUIT\n");
                string respuesta = cliente.LeerLinea(EsperaMs);
                string siguiente = cliente.LeerLinea(EsperaMs);

                caso.Obtenido = respuesta + " // " + siguiente;
                Finalizar(caso, respuesta == "201|BYE" && siguiente == ClienteCrudo.ConexionCerrada);
            }
            finally { cliente.Cerrar(); }
        }

        private void CN06(CasoPrueba caso)
        {
            ProbarErrorYContinuar(caso, "HELLO|9", "407|UNSUPPORTED_VERSION|", AyudaConectado);
        }

        // 255 caracteres + LF = 256 bytes: es el máximo permitido, se procesa normalmente
        private void CL05(CasoPrueba caso)
        {
            ClienteCrudo cliente = AbrirSesionIdentificada();
            try
            {
                cliente.EnviarTexto(new string('A', 255) + "\n");
                string respuesta = cliente.LeerLinea(EsperaMs);

                cliente.EnviarTexto("HELP\n");
                string ayuda = cliente.LeerLinea(EsperaMs);

                caso.Obtenido = respuesta + " // luego HELP -> " + ayuda;
                Finalizar(caso, Comienza(respuesta, "400|UNKNOWN_COMMAND|") && Comienza(ayuda, "202|HELP|"));
            }
            finally { cliente.Cerrar(); }
        }

        // 256 caracteres + LF = 257 bytes: supera el máximo; el servidor responde 403 y cierra
        private void CL06(CasoPrueba caso)
        {
            ClienteCrudo cliente = AbrirSesionIdentificada();
            try
            {
                cliente.EnviarTexto(new string('A', 256) + "\n");
                string respuesta = cliente.LeerLinea(EsperaMs);
                string siguiente = cliente.LeerLinea(EsperaMs);

                caso.Obtenido = respuesta + " // luego: " + siguiente;
                Finalizar(caso, Comienza(respuesta, "403|MESSAGE_TOO_LONG|")
                             && siguiente == ClienteCrudo.ConexionCerrada);
            }
            finally { cliente.Cerrar(); }
        }

        // "HEL" y "LO|1\n" llegan por separado: el servidor debe esperar el LF
        private void CR01(CasoPrueba caso)
        {
            ClienteCrudo cliente = AbrirSesionNueva();
            try
            {
                cliente.EnviarTexto("HEL");
                Thread.Sleep(1000);
                cliente.EnviarTexto("LO|1\n");

                string respuesta = cliente.LeerLinea(EsperaMs);
                string extra = cliente.LeerLinea(500);   // no debe llegar nada más

                caso.Obtenido = respuesta + " (y luego: " + extra + ")";
                Finalizar(caso, respuesta == Bienvenida && extra == ClienteCrudo.SinRespuesta);
            }
            finally { cliente.Cerrar(); }
        }

        // Dos mensajes pegados en un solo envío: el servidor debe responder a los dos, en orden
        private void CR02(CasoPrueba caso)
        {
            ClienteCrudo cliente = AbrirSesionNueva();
            try
            {
                cliente.EnviarTexto("HELLO|1\nHELP\n");
                List<string> lineas = LeerLineas(cliente, 2);

                caso.Obtenido = string.Join(" // ", lineas.ToArray());
                Finalizar(caso, lineas[0] == Bienvenida && Comienza(lineas[1], "202|HELP|"));
            }
            finally { cliente.Cerrar(); }
        }

        private void CR03(CasoPrueba caso)
        {
            ClienteCrudo cliente = AbrirSesionNueva();
            try
            {
                cliente.EnviarTexto("HELLO|1\r\n");
                string respuesta = cliente.LeerLinea(EsperaMs);
                caso.Obtenido = respuesta;
                Finalizar(caso, respuesta == Bienvenida);
            }
            finally { cliente.Cerrar(); }
        }

        private void CR04(CasoPrueba caso)
        {
            ClienteCrudo cliente = AbrirSesionNueva();
            try
            {
                cliente.EnviarTexto("hello|1\n");
                string primera = cliente.LeerLinea(EsperaMs);

                cliente.EnviarTexto("current|uio\n");
                string segunda = cliente.LeerLinea(EsperaMs);

                caso.Obtenido = primera + " // " + segunda;
                Finalizar(caso, primera == Bienvenida && Comienza(segunda, "203|CURRENT|"));
            }
            finally { cliente.Cerrar(); }
        }

        // Se corta la conexión de golpe (sin QUIT). Después se abre otra para comprobar
        // que el servidor sigue vivo. El registro de la desconexión se ve en la consola del servidor.
        private void CR05(CasoPrueba caso)
        {
            ClienteCrudo primero = AbrirSesionIdentificada();
            primero.CerrarAbruptamente();
            Thread.Sleep(500);

            ClienteCrudo segundo = AbrirSesionNueva();
            try
            {
                segundo.EnviarTexto("HELLO|1\n");
                string respuesta = segundo.LeerLinea(EsperaMs);

                caso.Obtenido = "Tras el cierre abrupto, la nueva conexión recibió: " + respuesta
                              + " (ver el registro de la desconexión en la consola del servidor)";
                Finalizar(caso, respuesta == Bienvenida);
            }
            finally { segundo.Cerrar(); }
        }
    }
}
