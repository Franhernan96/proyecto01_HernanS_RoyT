using System.Collections.Generic;
using System.Globalization;
using MeteoAula.Protocolo;
using MeteoAula.Servidor.Servicios;

namespace MeteoAula.Servidor.Interprete
{
    /// <summary>
    /// Interpreta cada línea recibida: la valida, aplica la máquina de estados,
    /// ejecuta el comando y devuelve la lista de líneas que hay que responder.
    /// Hay una instancia por cliente conectado, así cada uno tiene su propio estado.
    /// </summary>
    public class InterpreteProtocolo
    {
        private AnalizadorComandos analizador;
        private ServicioConsultas servicio;

        public EstadoSesion Estado { get; private set; }

        public bool SesionTerminada
        {
            get { return Estado == EstadoSesion.Cerrado; }
        }

        public InterpreteProtocolo(ServicioConsultas servicio)
        {
            this.servicio = servicio;
            this.analizador = new AnalizadorComandos();
            Estado = EstadoSesion.Conectado;   // toda sesión nace en Conectado
        }

        /// <summary>
        /// Procesa una línea y devuelve las líneas de respuesta.
        /// Orden de validación (definido en el contrato):
        /// línea vacía -> comando conocido -> permitido en el estado ->
        /// cantidad de parámetros -> formato -> versión -> rango -> existencia.
        /// </summary>
        public List<string> Procesar(string linea)
        {
            List<string> respuestas = new List<string>();

            try
            {
                // Pasos 2 y 3: línea vacía (402) y comando desconocido (400)
                Comando comando = analizador.Analizar(linea);

                // ---------------- MÁQUINA DE ESTADOS ----------------
                // Paso 4: antes de ejecutar nada se verifica que el comando esté
                // permitido en el estado actual. Antes de HELLO solo se admiten
                // HELLO, HELP y QUIT; cualquier otro da 406.
                if (!ComandoPermitido(comando.Nombre))
                {
                    if (Estado == EstadoSesion.Conectado)
                    {
                        throw new ExcepcionProtocolo(CodigosRespuesta.EstadoInvalido, "Debe identificarse con HELLO antes de usar este comando");
                    }
                    throw new ExcepcionProtocolo(CodigosRespuesta.EstadoInvalido, "El comando no está permitido en el estado actual");
                }

                switch (comando.Nombre)
                {
                    case NombresComandos.Hello:
                        respuestas = ProcesarHello(comando);
                        break;
                    case NombresComandos.Help:
                        respuestas = ProcesarHelp(comando);
                        break;
                    case NombresComandos.List:
                        respuestas = ProcesarList(comando);
                        break;
                    case NombresComandos.Quit:
                        respuestas = ProcesarQuit(comando);
                        break;
                    case NombresComandos.Current:
                        respuestas = ProcesarCurrent(comando);
                        break;
                    case NombresComandos.History:
                        respuestas = ProcesarHistory(comando);
                        break;
                    case NombresComandos.Summary:
                        respuestas = ProcesarSummary(comando);
                        break;
                }
            }
            catch (ExcepcionProtocolo ex)
            {
                // Los errores del protocolo NO cierran la sesión: se responde con una
                // línea de error y el cliente puede seguir enviando comandos.
                // Cualquier otra excepción sube hasta SesionCliente.
                respuestas = new List<string>();
                respuestas.Add(CrearLineaError(ex.Codigo, ex.Message));
            }

            return respuestas;
        }

        // ---------------- COMANDOS ----------------

        private List<string> ProcesarHello(Comando comando)
        {
            ValidarCantidadParametros(comando, 1);

            int version;
            if (!int.TryParse(comando.Parametros[0], NumberStyles.Integer,
                              CultureInfo.InvariantCulture, out version))
            {
                throw new ExcepcionProtocolo(CodigosRespuesta.FormatoInvalido, "La versión debe ser un número entero");
            }

            if (version != ConstantesProtocolo.Version)
            {
                throw new ExcepcionProtocolo(CodigosRespuesta.VersionNoSoportada, "Versión de protocolo no soportada");
            }

            // Transición Conectado -> Identificado -> Consultas.
            // Identificado es transitorio: se pasa por él y de inmediato se
            // habilitan las consultas al enviar el 200.
            Estado = EstadoSesion.Identificado;
            List<string> respuestas = new List<string>();
            respuestas.Add(ArmarLineaConEtiqueta(CodigosRespuesta.Bienvenida, ConstantesProtocolo.Version.ToString()));
            Estado = EstadoSesion.Consultas;
            return respuestas;
        }

        private List<string> ProcesarHelp(Comando comando)
        {
            ValidarCantidadParametros(comando, 0);

            // Solo se listan los comandos permitidos en el estado actual,
            // separados por ';' (para no confundirse con el separador '|')
            string comandos = string.Join(";", ObtenerComandosPermitidos());

            List<string> respuestas = new List<string>();
            respuestas.Add(ArmarLineaConEtiqueta(CodigosRespuesta.Ayuda, comandos));
            return respuestas;
        }

        private List<string> ProcesarList(Comando comando)
        {
            ValidarCantidadParametros(comando, 0);

            List<Estacion> estaciones = servicio.ObtenerEstaciones();
            List<string> respuestas = new List<string>();

            // Respuesta de varios registros:
            //   210|COUNT|n   -> cuántas filas vienen
            //   211|...       -> exactamente n filas
            //   219|END       -> el cliente sabe que terminó
            respuestas.Add(ArmarLineaConEtiqueta(CodigosRespuesta.Conteo, estaciones.Count.ToString()));

            foreach (Estacion estacion in estaciones)
            {
                respuestas.Add(ArmarLinea(CodigosRespuesta.FilaEstacion, estacion.Codigo, estacion.Nombre, estacion.Ciudad));
            }

            respuestas.Add(ArmarLineaConEtiqueta(CodigosRespuesta.Fin));
            return respuestas;
        }
        private List<string> ProcesarCurrent(Comando comando)
        {
            // Orden de validación: cantidad -> formato -> existencia de la estación
            ValidarCantidadParametros(comando, 1);
            string codigo = comando.Parametros[0];
            ValidarFormatoCodigo(codigo);
            VerificarEstacionExiste(codigo);

            Lectura lectura = servicio.ObtenerLecturaActual(codigo);
            if (lectura == null)
            {
                throw new ExcepcionProtocolo(CodigosRespuesta.ErrorInterno, "La estación no tiene lecturas");
            }

            List<string> respuestas = new List<string>();
            respuestas.Add(ArmarLineaConEtiqueta(CodigosRespuesta.LecturaActual,
                ConvertidorDatos.FechaHoraATexto(lectura.FechaHora),
                ConvertidorDatos.TemperaturaATexto(lectura.Temperatura),
                lectura.Humedad.ToString(),
                lectura.EstadoTiempo));
            return respuestas;
        }

        private List<string> ProcesarHistory(Comando comando)
        {
            // Orden de validación: cantidad -> formato (código y N) -> rango de N -> existencia
            ValidarCantidadParametros(comando, 2);

            string codigo = comando.Parametros[0];
            ValidarFormatoCodigo(codigo);

            int n;
            if (!int.TryParse(comando.Parametros[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out n))
            {
                throw new ExcepcionProtocolo(CodigosRespuesta.FormatoInvalido, "N debe ser un número entero");
            }

            // N máximo definido en el protocolo (ConstantesProtocolo.MaxRegistrosHistorial)
            if (n < 1 || n > ConstantesProtocolo.MaxRegistrosHistorial)
            {
                throw new ExcepcionProtocolo(CodigosRespuesta.FueraDeRango, "N debe estar entre 1 y " + ConstantesProtocolo.MaxRegistrosHistorial);
            }

            VerificarEstacionExiste(codigo);

            List<Lectura> lecturas = servicio.ObtenerHistorial(codigo, n);
            List<string> respuestas = new List<string>();

            // Respuesta de varios registros: COUNT, n filas y END.
            // COUNT lleva la cantidad REAL de filas (puede ser menor que N).
            respuestas.Add(ArmarLineaConEtiqueta(CodigosRespuesta.Conteo,
                                                 lecturas.Count.ToString()));

            foreach (Lectura lectura in lecturas)
            {
                respuestas.Add(ArmarLinea(CodigosRespuesta.FilaLectura,
                    ConvertidorDatos.FechaHoraATexto(lectura.FechaHora),
                    ConvertidorDatos.TemperaturaATexto(lectura.Temperatura),
                    lectura.Humedad.ToString(),
                    lectura.EstadoTiempo));
            }

            respuestas.Add(ArmarLineaConEtiqueta(CodigosRespuesta.Fin));
            return respuestas;
        }

        private List<string> ProcesarSummary(Comando comando)
        {
            ValidarCantidadParametros(comando, 1);
            string codigo = comando.Parametros[0];
            ValidarFormatoCodigo(codigo);
            VerificarEstacionExiste(codigo);

            ResumenTemperatura resumen = servicio.ObtenerResumen(codigo);
            if (resumen == null)
            {
                throw new ExcepcionProtocolo(CodigosRespuesta.ErrorInterno, "La estación no tiene lecturas");
            }

            List<string> respuestas = new List<string>();
            respuestas.Add(ArmarLineaConEtiqueta(CodigosRespuesta.Resumen,
                ConvertidorDatos.TemperaturaATexto(resumen.Promedio),
                ConvertidorDatos.TemperaturaATexto(resumen.Minimo),
                ConvertidorDatos.TemperaturaATexto(resumen.Maximo),
                resumen.CantidadLecturas.ToString()));
            return respuestas;
        }
        private List<string> ProcesarQuit(Comando comando)
        {
            ValidarCantidadParametros(comando, 0);

            Estado = EstadoSesion.Cerrado;   // SesionCliente cerrará el socket
            List<string> respuestas = new List<string>();
            respuestas.Add(ArmarLineaConEtiqueta(CodigosRespuesta.Despedida));
            return respuestas;
        }

        // ---------------- VALIDACIONES Y AYUDAS ----------------

        // Única fuente de verdad de qué comandos admite cada estado.
        // La usan tanto la validación como el comando HELP.
        private List<string> ObtenerComandosPermitidos()
        {
            List<string> permitidos = new List<string>();

            if (Estado == EstadoSesion.Conectado)
            {
                permitidos.Add(NombresComandos.Hello);
                permitidos.Add(NombresComandos.Help);
                permitidos.Add(NombresComandos.Quit);
            }
            else if (Estado == EstadoSesion.Identificado)
            {
                permitidos.Add(NombresComandos.Help);
                permitidos.Add(NombresComandos.Quit);
            }
            else if (Estado == EstadoSesion.Consultas)
            {
                permitidos.Add(NombresComandos.List);
                permitidos.Add(NombresComandos.Current);
                permitidos.Add(NombresComandos.History);
                permitidos.Add(NombresComandos.Summary);
                permitidos.Add(NombresComandos.Help);
                permitidos.Add(NombresComandos.Quit);
            }
            // En Cerrado no se permite ninguno

            return permitidos;
        }

        private bool ComandoPermitido(string nombre)
        {
            return ObtenerComandosPermitidos().Contains(nombre);
        }

        // Faltan parámetros -> 401; sobran parámetros -> 402
        private void ValidarCantidadParametros(Comando comando, int esperados)
        {
            if (comando.Parametros.Count < esperados)
            {
                throw new ExcepcionProtocolo(CodigosRespuesta.FaltanParametros, "Faltan parámetros");
            }
            if (comando.Parametros.Count > esperados)
            {
                throw new ExcepcionProtocolo(CodigosRespuesta.FormatoInvalido, "Sobran parámetros");
            }
        }
        // El código de estación no puede venir vacío (ej. "CURRENT|") -> 402
        private void ValidarFormatoCodigo(string codigo)
        {
            if (codigo.Length == 0)
            {
                throw new ExcepcionProtocolo(CodigosRespuesta.FormatoInvalido, "El código de estación está vacío");
            }
        }

        // Estación que no existe -> 404 (el mensaje no repite lo que envió el cliente)
        private void VerificarEstacionExiste(string codigo)
        {
            if (!servicio.ExisteEstacion(codigo))
            {
                throw new ExcepcionProtocolo(CodigosRespuesta.EstacionNoExiste, "La estación solicitada no existe");
            }
        }
        // Línea de error: "<codigo>|<ETIQUETA>|<descripción>"
        // Es pública y estática porque SesionCliente también la usa (403 y 500).
        // Regla: la descripción nunca repite lo que escribió el cliente, así una
        // respuesta jamás supera el máximo de 256 bytes.
        public static string CrearLineaError(int codigo, string descripcion)
        {
            // Si la descripción trajera un '|' rompería el formato de campos
            string limpia = descripcion.Replace(ConstantesProtocolo.SeparadorCampos, '/');
            return ArmarLineaConEtiqueta(codigo, limpia);
        }

        // "<codigo>|<ETIQUETA>|campo1|campo2..."
        private static string ArmarLineaConEtiqueta(int codigo, params string[] campos)
        {
            string linea = codigo.ToString() + ConstantesProtocolo.SeparadorCampos
                         + CodigosRespuesta.ObtenerEtiqueta(codigo);
            foreach (string campo in campos)
            {
                linea = linea + ConstantesProtocolo.SeparadorCampos + campo;
            }
            return linea;
        }

        // "<codigo>|campo1|campo2..."  (filas de datos, sin etiqueta)
        private static string ArmarLinea(int codigo, params string[] campos)
        {
            string linea = codigo.ToString();
            foreach (string campo in campos)
            {
                linea = linea + ConstantesProtocolo.SeparadorCampos + campo;
            }
            return linea;
        }
    }
}