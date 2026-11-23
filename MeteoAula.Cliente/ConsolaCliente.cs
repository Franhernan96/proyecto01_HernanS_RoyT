using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using MeteoAula.Protocolo;

namespace MeteoAula.Cliente
{
    /// <summary>
    /// Interfaz de consola: muestra el menú, arma las solicitudes del protocolo
    /// e interpreta las respuestas para mostrarlas de forma estructurada.
    /// </summary>
    public class ConsolaCliente
    {
        private ConexionCliente conexion;
        private AnalizadorRespuestas analizador;
        private string ip;
        private int puerto;

        public ConsolaCliente(string ip, int puerto)
        {
            this.ip = ip;
            this.puerto = puerto;
            this.conexion = new ConexionCliente();
            this.analizador = new AnalizadorRespuestas();
        }

        public void Ejecutar()
        {
            bool salir = false;

            while (!salir)
            {
                MostrarMenu();
                string opcion = Console.ReadLine();
                if (opcion == null)
                {
                    opcion = "0";   // fin de la entrada (Ctrl+Z): se sale
                }

                try
                {
                    switch (opcion.Trim())
                    {
                        case "1": ConfigurarServidor(); break;
                        case "2": Conectar(); break;
                        case "3": ListarEstaciones(); break;
                        case "4": ConsultarLecturaActual(); break;
                        case "5": ConsultarHistorial(); break;
                        case "6": ConsultarResumen(); break;
                        case "7": ConsultarAyuda(); break;
                        case "8": EnviarComandoManual(); break;
                        case "9": Desconectar(); break;
                        case "0":
                            if (conexion.Conectado)
                            {
                                Desconectar();
                            }
                            salir = true;
                            break;
                        default:
                            Console.WriteLine("Opción no válida.");
                            break;
                    }
                }
                // ---------- ERRORES DE RED Y DE PROTOCOLO ----------
                // La respuesta del servidor no cumple el protocolo (por ejemplo, anunció
                // 3 filas y envió 2). Los bytes que quedan en el flujo ya no son
                // confiables, así que se cierra la conexión y se pide reconectar.
                catch (ExcepcionProtocolo ex)
                {
                    conexion.Desconectar();
                    Console.WriteLine("Error de protocolo: " + ex.Message);
                    Console.WriteLine("Se cerró la conexión por seguridad. Use la opción 2 para volver a conectar.");
                }
                // El servidor se cayó, cerró la conexión o dejó de responder:
                // se detecta aquí y se permite reconectar manualmente.
                catch (IOException ex)
                {
                    conexion.Desconectar();
                    Console.WriteLine("Se perdió la comunicación con el servidor (" + ex.Message + ").");
                    Console.WriteLine("Use la opción 2 para conectarse de nuevo.");
                }
                catch (SocketException ex)
                {
                    conexion.Desconectar();
                    Console.WriteLine("Error de red (" + ex.Message + ").");
                    Console.WriteLine("Use la opción 2 para conectarse de nuevo.");
                }
            }

            Console.WriteLine("Hasta luego.");
        }

        private void MostrarMenu()
        {
            Console.WriteLine();
            Console.WriteLine("=== Observatorio Remoto MeteoAula ===");
            if (conexion.Conectado)
            {
                Console.WriteLine("Estado: CONECTADO a {0}:{1}", ip, puerto);
            }
            else
            {
                Console.WriteLine("Estado: DESCONECTADO (servidor configurado: {0}:{1})", ip, puerto);
            }
            Console.WriteLine("1. Configurar IP y puerto");
            Console.WriteLine("2. Conectar");
            Console.WriteLine("3. Listar estaciones (LIST)");
            Console.WriteLine("4. Lectura más reciente (CURRENT)");
            Console.WriteLine("5. Historial de lecturas (HISTORY)");
            Console.WriteLine("6. Resumen de temperatura (SUMMARY)");
            Console.WriteLine("7. Ayuda del servidor (HELP)");
            Console.WriteLine("8. Enviar comando manual (pruebas)");
            Console.WriteLine("9. Desconectar (QUIT)");
            Console.WriteLine("0. Salir");
            Console.Write("Opción: ");
        }

        // ---------------- CONEXIÓN ----------------

        private void ConfigurarServidor()
        {
            if (conexion.Conectado)
            {
                Console.WriteLine("Desconéctese primero (opción 9) para cambiar la IP o el puerto.");
                return;
            }

            string nuevaIp = LeerTexto("IP del servidor [" + ip + "]: ");
            if (nuevaIp.Length > 0)
            {
                ip = nuevaIp;
            }

            string textoPuerto = LeerTexto("Puerto [" + puerto + "]: ");
            if (textoPuerto.Length > 0)
            {
                int nuevoPuerto;
                if (int.TryParse(textoPuerto, NumberStyles.None, CultureInfo.InvariantCulture, out nuevoPuerto)
                    && nuevoPuerto >= 1 && nuevoPuerto <= 65535)
                {
                    puerto = nuevoPuerto;
                }
                else
                {
                    Console.WriteLine("Puerto inválido; se mantiene {0}.", puerto);
                }
            }
        }

        private void Conectar()
        {
            if (conexion.Conectado)
            {
                Console.WriteLine("Ya hay una conexión activa. Use la opción 9 para cerrarla.");
                return;
            }

            Console.WriteLine("Conectando a {0}:{1}...", ip, puerto);

            try
            {
                conexion.Conectar(ip, puerto);
            }
            catch (SocketException)
            {
                MostrarFalloDeConexion();
                return;
            }
            catch (IOException)
            {
                MostrarFalloDeConexion();
                return;
            }

            // Primer mensaje del protocolo: HELLO con la versión
            Respuesta respuesta = EnviarComando(ArmarComando(NombresComandos.Hello,
                                                ConstantesProtocolo.Version.ToString()));

            if (respuesta.Codigo == CodigosRespuesta.Bienvenida)
            {
                Console.WriteLine("Conexión establecida. Protocolo {0} versión {1}.",
                                  ConstantesProtocolo.Nombre, ConstantesProtocolo.Version);
            }
            else
            {
                MostrarError(respuesta);
                conexion.Desconectar();
            }
        }

        private void MostrarFalloDeConexion()
        {
            Console.WriteLine("No se pudo conectar con {0}:{1}.", ip, puerto);
            Console.WriteLine("Verifique que el servidor esté en ejecución, que la IP y el puerto sean");
            Console.WriteLine("correctos y que el firewall permita la conexión.");
        }

        private void Desconectar()
        {
            if (!conexion.Conectado)
            {
                Console.WriteLine("No hay una conexión activa.");
                return;
            }

            try
            {
                Respuesta respuesta = EnviarComando(NombresComandos.Quit);
                if (respuesta.Codigo == CodigosRespuesta.Despedida)
                {
                    Console.WriteLine("Sesión cerrada correctamente.");
                }
                else
                {
                    MostrarError(respuesta);
                }
            }
            catch (IOException)
            {
                Console.WriteLine("La conexión ya se había perdido.");
            }
            catch (ExcepcionProtocolo)
            {
                Console.WriteLine("Respuesta inesperada del servidor al cerrar.");
            }
            finally
            {
                conexion.Desconectar();   // siempre se libera el socket
            }
        }

        // ---------------- CONSULTAS ----------------

        private void ListarEstaciones()
        {
            if (!HayConexion()) { return; }

            Respuesta inicio = EnviarComando(NombresComandos.List);
            if (!inicio.EsExito())
            {
                MostrarError(inicio);
                return;
            }

            // Respuesta de varios registros: se lee COUNT, luego las filas, luego END
            int cantidad = analizador.ConvertirACantidad(inicio);
            List<Respuesta> filas = LeerFilas(cantidad, CodigosRespuesta.FilaEstacion);

            Console.WriteLine();
            Console.WriteLine("{0,-8}{1,-28}{2}", "Código", "Nombre", "Ciudad");
            foreach (Respuesta fila in filas)
            {
                Estacion estacion = analizador.ConvertirAEstacion(fila);
                Console.WriteLine("{0,-8}{1,-28}{2}", estacion.Codigo, estacion.Nombre, estacion.Ciudad);
            }
            Console.WriteLine("Total: {0} estaciones.", cantidad);
        }

        private void ConsultarLecturaActual()
        {
            if (!HayConexion()) { return; }

            string codigo = PedirCodigoEstacion();
            if (codigo == null) { return; }

            Respuesta respuesta = EnviarComando(ArmarComando(NombresComandos.Current, codigo));
            if (!respuesta.EsExito())
            {
                MostrarError(respuesta);
                return;
            }

            Lectura lectura = analizador.ConvertirALectura(respuesta);
            Console.WriteLine();
            Console.WriteLine("Lectura más reciente de {0}:", codigo.ToUpperInvariant());
            Console.WriteLine("  Fecha y hora : {0}", ConvertidorDatos.FechaHoraATexto(lectura.FechaHora));
            Console.WriteLine("  Temperatura  : {0} °C", ConvertidorDatos.TemperaturaATexto(lectura.Temperatura));
            Console.WriteLine("  Humedad      : {0} %", lectura.Humedad);
            Console.WriteLine("  Estado       : {0}", lectura.EstadoTiempo);
        }

        private void ConsultarHistorial()
        {
            if (!HayConexion()) { return; }

            string codigo = PedirCodigoEstacion();
            if (codigo == null) { return; }

            // El cliente solo comprueba que N sea un número entero. El rango (1 a 10)
            // lo valida el servidor, así se puede ver el error 405 en las pruebas.
            string textoN = LeerTexto("Cantidad de registros (N): ");
            int n;
            if (!int.TryParse(textoN, NumberStyles.Integer, CultureInfo.InvariantCulture, out n))
            {
                Console.WriteLine("N debe ser un número entero.");
                return;
            }

            Respuesta inicio = EnviarComando(ArmarComando(NombresComandos.History, codigo,
                                             n.ToString(CultureInfo.InvariantCulture)));
            if (!inicio.EsExito())
            {
                MostrarError(inicio);
                return;
            }

            int cantidad = analizador.ConvertirACantidad(inicio);
            List<Respuesta> filas = LeerFilas(cantidad, CodigosRespuesta.FilaLectura);

            Console.WriteLine();
            Console.WriteLine("Historial de {0} ({1} registros):", codigo.ToUpperInvariant(), cantidad);
            Console.WriteLine("{0,-18}{1,10}{2,13}  {3}", "Fecha y hora", "Temp (°C)", "Humedad (%)", "Estado");
            foreach (Respuesta fila in filas)
            {
                Lectura lectura = analizador.ConvertirALectura(fila);
                Console.WriteLine("{0,-18}{1,10}{2,13}  {3}",
                    ConvertidorDatos.FechaHoraATexto(lectura.FechaHora),
                    ConvertidorDatos.TemperaturaATexto(lectura.Temperatura),
                    lectura.Humedad,
                    lectura.EstadoTiempo);
            }
        }

        private void ConsultarResumen()
        {
            if (!HayConexion()) { return; }

            string codigo = PedirCodigoEstacion();
            if (codigo == null) { return; }

            Respuesta respuesta = EnviarComando(ArmarComando(NombresComandos.Summary, codigo));
            if (!respuesta.EsExito())
            {
                MostrarError(respuesta);
                return;
            }

            ResumenTemperatura resumen = analizador.ConvertirAResumen(respuesta);
            Console.WriteLine();
            Console.WriteLine("Resumen de temperatura de {0}:", codigo.ToUpperInvariant());
            Console.WriteLine("  Promedio : {0} °C", ConvertidorDatos.TemperaturaATexto(resumen.Promedio));
            Console.WriteLine("  Mínimo   : {0} °C", ConvertidorDatos.TemperaturaATexto(resumen.Minimo));
            Console.WriteLine("  Máximo   : {0} °C", ConvertidorDatos.TemperaturaATexto(resumen.Maximo));
            Console.WriteLine("  Lecturas : {0}", resumen.CantidadLecturas);
        }

        private void ConsultarAyuda()
        {
            if (!HayConexion()) { return; }

            Respuesta respuesta = EnviarComando(NombresComandos.Help);
            if (!respuesta.EsExito())
            {
                MostrarError(respuesta);
                return;
            }

            if (respuesta.Codigo != CodigosRespuesta.Ayuda || respuesta.Campos.Count != 1)
            {
                throw new ExcepcionProtocolo(CodigosRespuesta.FormatoInvalido,
                    "Respuesta de ayuda inválida");
            }

            Console.WriteLine("Comandos disponibles ahora: " + string.Join(", ", respuesta.Campos[0].Split(';')));
        }

        // Envía el texto tal cual, sin validar nada. Sirve para probar los errores
        // del servidor (comando desconocido, línea vacía, mensaje muy largo, etc.).
        private void EnviarComandoManual()
        {
            if (!HayConexion()) { return; }

            Console.Write("Comando a enviar tal cual: ");
            string texto = Console.ReadLine();
            if (texto == null) { return; }

            Respuesta respuesta = EnviarComando(texto);
            Console.WriteLine("<- " + respuesta.TextoOriginal);

            // Si el servidor anuncia una lista, se muestran todas sus líneas hasta el 219
            if (respuesta.Codigo == CodigosRespuesta.Conteo)
            {
                while (conexion.Conectado)
                {
                    Respuesta fila = LeerRespuesta();
                    Console.WriteLine("<- " + fila.TextoOriginal);
                    if (fila.Codigo == CodigosRespuesta.Fin)
                    {
                        break;
                    }
                }
            }
        }

        // ---------------- COMUNICACIÓN CON EL SERVIDOR ----------------

        // Arma una solicitud del protocolo: NOMBRE|parametro1|parametro2
        private string ArmarComando(string nombre, params string[] parametros)
        {
            string comando = nombre;
            foreach (string parametro in parametros)
            {
                comando = comando + ConstantesProtocolo.SeparadorCampos + parametro;
            }
            return comando;
        }

        private Respuesta EnviarComando(string comando)
        {
            conexion.EnviarLinea(comando);
            return LeerRespuesta();
        }

        private Respuesta LeerRespuesta()
        {
            string linea = conexion.LeerLinea();             // IOException si el servidor cerró
            Respuesta respuesta = analizador.Analizar(linea);   // ExcepcionProtocolo si es inválida

            // El servidor cierra su lado después de QUIT (201) y después de un
            // mensaje demasiado largo (403): el cliente también cierra el suyo.
            if (respuesta.Codigo == CodigosRespuesta.Despedida
                || respuesta.Codigo == CodigosRespuesta.MensajeMuyLargo)
            {
                conexion.Desconectar();
            }
            return respuesta;
        }

        /// <summary>
        /// Lee las filas de una respuesta de varios registros y comprueba que
        /// sea coherente: el cliente sabe que terminó al recibir "219|END", y
        /// además verifica que la cantidad de filas coincida con el COUNT anunciado.
        /// Las filas se devuelven recién cuando toda la respuesta fue validada,
        /// así nunca se muestran datos de una respuesta incompleta.
        /// </summary>
        private List<Respuesta> LeerFilas(int cantidadAnunciada, int codigoFila)
        {
            List<Respuesta> filas = new List<Respuesta>();

            while (true)
            {
                Respuesta respuesta = LeerRespuesta();

                if (respuesta.Codigo == CodigosRespuesta.Fin)
                {
                    break;   // fin de la lista
                }

                if (respuesta.Codigo != codigoFila)
                {
                    throw new ExcepcionProtocolo(CodigosRespuesta.FormatoInvalido,
                        "Se esperaba una fila de datos o el fin de la lista (219)");
                }

                filas.Add(respuesta);

                if (filas.Count > cantidadAnunciada)
                {
                    throw new ExcepcionProtocolo(CodigosRespuesta.FormatoInvalido,
                        "El servidor envió más filas de las anunciadas");
                }
            }

            if (filas.Count != cantidadAnunciada)
            {
                throw new ExcepcionProtocolo(CodigosRespuesta.FormatoInvalido,
                    string.Format("El servidor anunció {0} filas pero envió {1}",
                                  cantidadAnunciada, filas.Count));
            }

            return filas;
        }

        // ---------------- AYUDAS ----------------

        private bool HayConexion()
        {
            if (!conexion.Conectado)
            {
                Console.WriteLine("No hay conexión con el servidor. Use la opción 2 para conectarse.");
                return false;
            }
            return true;
        }

        // Muestra un error del protocolo en lenguaje comprensible
        private void MostrarError(Respuesta respuesta)
        {
            string descripcion = "";
            if (respuesta.Campos.Count > 0)
            {
                descripcion = respuesta.Campos[0];
            }

            Console.WriteLine("El servidor respondió con un error [{0} {1}]: {2}",
                              respuesta.Codigo, respuesta.Etiqueta, descripcion);

            if (respuesta.Codigo == CodigosRespuesta.MensajeMuyLargo)
            {
                Console.WriteLine("El servidor cerró la conexión. Use la opción 2 para reconectar.");
            }
        }

        private string LeerTexto(string pregunta)
        {
            Console.Write(pregunta);
            string texto = Console.ReadLine();
            if (texto == null)
            {
                return "";
            }
            return texto.Trim();
        }

        // Pide el código de estación y comprueba lo básico antes de armar la solicitud
        private string PedirCodigoEstacion()
        {
            string codigo = LeerTexto("Código de estación (ej. UIO): ");

            if (codigo.Length == 0)
            {
                Console.WriteLine("Debe ingresar un código de estación.");
                return null;
            }
            if (codigo.IndexOf(ConstantesProtocolo.SeparadorCampos) >= 0)
            {
                Console.WriteLine("El código no puede contener el carácter '|'.");
                return null;
            }
            return codigo;
        }
    }
}