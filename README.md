# MeteoAula – Observatorio Remoto

Protocolo de aplicación propio (**MTP/1**, *MeteoAula Text Protocol*) sobre sockets TCP, con un servidor y un cliente de consola escritos en C#.

Proyecto 01 – 2026B · ITID713 Aplicaciones Distribuidas · Carrera en Tecnologías de la Información · Facultad de Ingeniería Eléctrica y Electrónica.

## Contenido

1. [Descripción](#descripción)
2. [Arquitectura](#arquitectura)
3. [Estructura del repositorio](#estructura-del-repositorio)
4. [Requisitos y dependencias](#requisitos-y-dependencias)
5. [Compilación](#compilación)
6. [Ejecución e IP/puerto](#ejecución-e-ippuerto)
7. [Uso del cliente](#uso-del-cliente)
8. [Protocolo MTP/1 (resumen)](#protocolo-mtp1-resumen)
9. [Prueba rápida sin el cliente](#prueba-rápida-sin-el-cliente)
10. [Pruebas automáticas](#pruebas-automáticas)
11. [Documentación](#documentación)
12. [Alcance y limitaciones](#alcance-y-limitaciones)
13. [Autores](#autores)

## Descripción

La empresa ficticia MeteoAula administra un observatorio con estaciones meteorológicas. Este proyecto permite que una aplicación cliente consulte, desde otra computadora, las estaciones disponibles, sus mediciones y algunos resúmenes. El objetivo no es pronosticar el clima, sino **diseñar e implementar el protocolo de comunicación** entre cliente y servidor.

- El servidor mantiene **5 estaciones** con **12 lecturas** cada una (una por hora, de 08:00 a 19:00 del 2026-10-01), precargadas en memoria.
- Todas las consultas son de **solo lectura**; no hay autenticación ni base de datos.
- Se atiende **un cliente a la vez**.
- Solo se usan sockets TCP (`TcpListener` / `TcpClient`). No se usa HTTP, REST ni librerías de mensajería.

## Arquitectura

```
Cliente de consola <-> Socket TCP <-> Servidor TCP -> Intérprete del protocolo
                                                   -> Servicio de consultas
                                                   -> Repositorio (memoria)
```

| Capa | Servidor | Cliente |
|---|---|---|
| Presentación | Consola (registro de conexiones y comandos) | `ConsolaCliente` |
| Transporte | `ServidorTcp`, `SesionCliente`, `LectorLineas` | `ConexionCliente` |
| Protocolo | `InterpreteProtocolo`, `EstadoSesion` | `AnalizadorRespuestas`, `Respuesta` |
| Lógica de consultas | `ServicioConsultas` | – |
| Datos | `IRepositorioLecturas`, `RepositorioMemoria` | – |

El proyecto `MeteoAula.Protocolo` contiene lo que comparten cliente y servidor: constantes, códigos, comandos, modelos y conversión de datos. El proyecto `MeteoAula.Pruebas` es independiente: habla con el servidor por un socket propio, sin usar el cliente ni la biblioteca compartida.

## Estructura del repositorio

```
MeteoAula/
├── README.md
├── .gitignore
├── MeteoAula.sln
├── MeteoAula.Protocolo/          Biblioteca de clases: contrato compartido
│   ├── ConstantesProtocolo.cs
│   ├── CodigosRespuesta.cs       (NombresComandos y CodigosRespuesta)
│   ├── ExcepcionProtocolo.cs
│   ├── Comando.cs
│   ├── AnalizadorComandos.cs
│   ├── Estacion.cs
│   ├── Lectura.cs
│   ├── ResumenTemperatura.cs
│   └── ConvertidorDatos.cs
├── MeteoAula.Servidor/           Aplicación de consola: servidor TCP
│   ├── Programa.cs
│   ├── Transporte/               LectorLineas, SesionCliente, ServidorTcp
│   ├── Interprete/               EstadoSesion, InterpreteProtocolo
│   ├── Servicios/                ServicioConsultas
│   └── Datos/                    IRepositorioLecturas, RepositorioMemoria
├── MeteoAula.Cliente/            Aplicación de consola: cliente
│   ├── Programa.cs
│   ├── ConsolaCliente.cs
│   ├── ConexionCliente.cs
│   ├── AnalizadorRespuestas.cs
│   └── Respuesta.cs
├── MeteoAula.Pruebas/            Aplicación de consola: pruebas contra el servidor
│   ├── Programa.cs
│   ├── EjecutorPruebas.cs
│   ├── ClienteCrudo.cs
│   ├── CasoPrueba.cs
│   ├── ReportadorResultados.cs
│   └── ServidorSimulado.cs
└── docs/                         Documentación del proyecto
    ├── logbook/                  Logbook con la especificación del protocolo, las pruebas y la demostración
    ├── diagramas/                Imágenes de los diagramas
    ├── pruebas/                  Resultados de las pruebas (resultados_pruebas.txt y .csv)
    └── informe/                  Informe ejecutivo y anexo de diagramas
```

## Requisitos y dependencias

| Requisito | Detalle |
|---|---|
| Sistema operativo | Windows 10 (1803 o posterior) o Windows 11 |
| Lenguaje y framework | C# sobre **.NET Framework 4.7.2** |
| Para compilar | **Visual Studio 2019 o 2022** con la carga de trabajo *Desarrollo de escritorio con .NET* (incluye el paquete de destino de .NET Framework 4.7.2) |
| Para ejecutar | Solo el runtime de .NET Framework 4.7.2, que ya viene con Windows 10 (1803+) y Windows 11 |
| Paquetes NuGet | **Ninguno.** El proyecto solo usa clases de la biblioteca estándar (`System.Net.Sockets`) |
| Red | Puerto TCP **8080** libre en el equipo servidor |

## Compilación

**Con Visual Studio**

1. Abre `MeteoAula.sln`.
2. Selecciona la configuración **Release** (o Debug).
3. Menú **Compilar → Compilar solución** (`Ctrl+Mayús+B`).

**Desde la línea de comandos** (*Developer Command Prompt for Visual Studio*):

```
msbuild MeteoAula.sln /p:Configuration=Release
```

Los ejecutables quedan en:

```
MeteoAula.Servidor\bin\Release\MeteoAula.Servidor.exe
MeteoAula.Cliente\bin\Release\MeteoAula.Cliente.exe
MeteoAula.Pruebas\bin\Release\MeteoAula.Pruebas.exe
```

También hay ejecutables ya compilados en la sección **Releases** del repositorio (`v1.0`).

## Ejecución e IP/puerto

| Parámetro | Valor por defecto | Cómo cambiarlo |
|---|---|---|
| Puerto del servidor | **8080** | `MeteoAula.Servidor.exe 9000` |
| IP en la que escucha el servidor | Todas las interfaces (`0.0.0.0`) | Fija en el código (`IPAddress.Any`) |
| IP del servidor (cliente) | **127.0.0.1** | Argumento o opción 1 del menú |
| Puerto del servidor (cliente) | **8080** | Argumento o opción 1 del menú |

### 1. Iniciar el servidor

```
MeteoAula.Servidor.exe
```

Debe mostrar:

```
Servidor MeteoAula escuchando en el puerto 8080. Ctrl+C para detener.
```

El servidor registra en consola cada conexión, cada comando recibido y cada respuesta enviada. Con `Ctrl+C` se detiene.

### 2. Iniciar el cliente

```
MeteoAula.Cliente.exe                      (usa 127.0.0.1 y 8080)
MeteoAula.Cliente.exe 192.168.1.20 8080    (IP y puerto indicados)
```

### 3. Prueba entre dos equipos de la misma red

1. En el **equipo servidor**, averigua su IP con `ipconfig` (campo *Dirección IPv4*).
2. Permite el puerto en el firewall de Windows (consola como administrador):

   ```
   netsh advfirewall firewall add rule name="MeteoAula 8080" dir=in action=allow protocol=TCP localport=8080
   ```

3. Inicia el servidor en ese equipo.
4. En el **equipo cliente**, ejecuta `MeteoAula.Cliente.exe <IP-del-servidor> 8080`, o usa la opción 1 del menú.

### Ejecutar desde Visual Studio

Clic derecho en la solución → *Configurar proyectos de inicio* → *Varios proyectos* → `MeteoAula.Servidor` y `MeteoAula.Cliente` en *Iniciar sin depurar* (con el servidor primero).

## Uso del cliente

```
=== Observatorio Remoto MeteoAula ===
Estado: DESCONECTADO (servidor configurado: 127.0.0.1:8080)
1. Configurar IP y puerto
2. Conectar
3. Listar estaciones (LIST)
4. Lectura más reciente (CURRENT)
5. Historial de lecturas (HISTORY)
6. Resumen de temperatura (SUMMARY)
7. Ayuda del servidor (HELP)
8. Enviar comando manual (pruebas)
9. Desconectar (QUIT)
0. Salir
```

- La opción **2** abre la conexión y envía `HELLO|1` automáticamente.
- La opción **8** envía el texto tal cual, sin validarlo, para probar los errores del servidor (comando desconocido, línea vacía, mensaje demasiado largo, etc.).
- Si el servidor se cae o deja de responder, el cliente lo detecta, lo informa y permite reconectar con la opción 2.

## Protocolo MTP/1 (resumen)

La especificación completa (gramática, estados, ejemplos, códigos, errores y límites) está en `docs/logbook/`.

### Formato

| Aspecto | Valor |
|---|---|
| Transporte | TCP, puerto 8080 |
| Codificación | UTF-8 sin BOM |
| Fin de mensaje | LF (`\n`); se tolera CR+LF |
| Separador de campos | `\|` |
| Tamaño máximo | 256 bytes por mensaje, incluido el LF |
| Versión | 1 (`HELLO\|1`) |
| Fecha y hora | `yyyy-MM-ddTHH:mm` |
| Temperatura | Punto decimal y un decimal (`18.5`) |
| Comandos | No distinguen mayúsculas; los códigos de estación tampoco |
| Inactividad | El servidor cierra la sesión tras 5 minutos sin recibir datos |

### Comandos (cliente → servidor)

| Comando | Sintaxis | Estado requerido | Respuesta exitosa |
|---|---|---|---|
| HELLO | `HELLO\|<version>` | Conectado | `200\|WELCOME\|1` |
| HELP | `HELP` | Cualquiera | `202\|HELP\|<comandos>` |
| LIST | `LIST` | Consultas | `210\|COUNT\|n`, n filas `211`, `219\|END` |
| CURRENT | `CURRENT\|<codigo>` | Consultas | `203\|CURRENT\|<fecha>\|<temp>\|<hum>\|<estado>` |
| HISTORY | `HISTORY\|<codigo>\|<n>` | Consultas | `210\|COUNT\|n`, n filas `212`, `219\|END` |
| SUMMARY | `SUMMARY\|<codigo>` | Consultas | `204\|SUMMARY\|<prom>\|<min>\|<max>\|<cantidad>` |
| QUIT | `QUIT` | Cualquiera | `201\|BYE` y cierre de la conexión |

`<n>` en HISTORY debe estar entre **1 y 10**. Devuelve las N lecturas más recientes, de la más antigua a la más reciente.

### Respuestas de varios registros

El cliente sabe dónde termina una respuesta de varios registros así:

1. El servidor envía `210|COUNT|n`.
2. Luego envía exactamente `n` filas (`211` en LIST, `212` en HISTORY).
3. Termina con `219|END`.

El cliente valida que la cantidad de filas coincida con `n`. Un error nunca lleva COUNT ni END: es una sola línea.

### Códigos de respuesta

| Código | Etiqueta | Significado |
|---|---|---|
| 200 | WELCOME | Identificación aceptada |
| 201 | BYE | Sesión cerrada |
| 202 | HELP | Lista de comandos disponibles |
| 203 | CURRENT | Lectura más reciente |
| 204 | SUMMARY | Promedio, mínimo y máximo |
| 210 | COUNT | Inicio de respuesta de varios registros |
| 211 | (fila) | Estación: `codigo\|nombre\|ciudad` |
| 212 | (fila) | Lectura: `fecha\|temp\|hum\|estado` |
| 219 | END | Fin de respuesta de varios registros |
| 400 | UNKNOWN_COMMAND | Comando desconocido |
| 401 | MISSING_PARAMS | Faltan parámetros |
| 402 | INVALID_FORMAT | Formato inválido, línea vacía o parámetros de más |
| 403 | MESSAGE_TOO_LONG | Mensaje de más de 256 bytes (el servidor cierra la conexión) |
| 404 | STATION_NOT_FOUND | La estación no existe |
| 405 | OUT_OF_RANGE | N fuera del rango 1 a 10 |
| 406 | INVALID_STATE | Comando no permitido en el estado actual |
| 407 | UNSUPPORTED_VERSION | Versión de protocolo no soportada |
| 500 | INTERNAL_ERROR | Error interno del servidor |

### Estados de la sesión

```
CONECTADO --HELLO|1--> IDENTIFICADO --(inmediato)--> CONSULTAS
    |                        |                          |
    +---- QUIT / desconexión / mensaje muy largo -------+--> CERRADO
```

Antes de `HELLO` solo se admiten `HELLO`, `HELP` y `QUIT`.

### Ejemplo de sesión

```
C -> S: HELLO|1
S -> C: 200|WELCOME|1
C -> S: HISTORY|UIO|2
S -> C: 210|COUNT|2
S -> C: 212|2026-10-01T18:00|17.9|66|LLUVIOSO
S -> C: 212|2026-10-01T19:00|17.2|68|NUBLADO
S -> C: 219|END
C -> S: SUMMARY|UIO
S -> C: 204|SUMMARY|19.1|16.8|21.4|12
C -> S: CURRENT|XXX
S -> C: 404|STATION_NOT_FOUND|La estación solicitada no existe
C -> S: HISTORY|UIO|99
S -> C: 405|OUT_OF_RANGE|N debe estar entre 1 y 10
C -> S: QUIT
S -> C: 201|BYE
```

### Estaciones precargadas

| Código | Nombre | Ciudad |
|---|---|---|
| UIO | Mariscal Sucre | Quito |
| GYE | José Joaquín de Olmedo | Guayaquil |
| CUE | Mariscal Lamar | Cuenca |
| MEC | Eloy Alfaro | Manta |
| LTX | Cotopaxi | Latacunga |

## Prueba rápida sin el cliente

Con el servidor en ejecución, desde PowerShell:

```powershell
$c = New-Object System.Net.Sockets.TcpClient("127.0.0.1", 8080)
$s = $c.GetStream()
$w = New-Object System.IO.StreamWriter($s); $w.AutoFlush = $true
$r = New-Object System.IO.StreamReader($s)

$w.Write("HELLO|1`n");        $r.ReadLine()                  # 200|WELCOME|1
$w.Write("CURRENT|UIO`n");    $r.ReadLine()                  # 203|CURRENT|2026-10-01T19:00|17.2|68|NUBLADO
$w.Write("QUIT`n");           $r.ReadLine()                  # 201|BYE
$c.Close()
```

Para probar un mensaje fragmentado: `$w.Write("HEL"); Start-Sleep 1; $w.Write("LO|1`n")` produce una sola respuesta `200`.

## Pruebas automáticas

El proyecto `MeteoAula.Pruebas` ejecuta contra el servidor real **28 de los 30 casos** del plan de pruebas (positivos, negativos, límites y robustez). Envía los bytes exactos de cada caso sin validarlos, por lo que también prueba mensajes de 255 y 256 caracteres, mensajes fragmentados o pegados, CR+LF y cierres abruptos.

1. Inicia el servidor y cierra cualquier cliente conectado (el servidor atiende uno a la vez).
2. Ejecuta el programa de pruebas (por defecto contra `127.0.0.1:8080`):

   ```
   MeteoAula.Pruebas.exe
   MeteoAula.Pruebas.exe 192.168.1.20 8080     (otro equipo)
   ```

3. Elige la opción **1**. Cada caso se muestra como APROBADO o FALLIDO, con entrada, resultado esperado y resultado obtenido.
4. Al terminar se guardan `resultados_pruebas.txt` y `resultados_pruebas.csv` junto al ejecutable. Los de la última ejecución están en `docs/pruebas/`.

Los 2 casos restantes se hacen a mano:

- **CR-06, servidor caído:** conecta el cliente, cierra el servidor con `Ctrl+C`, envía un comando (el cliente avisa de la pérdida) y reconecta tras reiniciar el servidor.
- **CR-07, respuesta incoherente:** en el programa de pruebas elige la opción **2** (servidor simulado en el puerto 8081, que anuncia 3 filas y envía 2) y conecta `MeteoAula.Cliente.exe 127.0.0.1 8081`; con la opción 3 (LIST) el cliente informa error de protocolo.

Resultado de la última ejecución: **30 casos, 30 aprobados, 0 fallidos** (28 automáticos y 2 manuales).

## Documentación

| Documento | Ubicación |
|---|---|
| Logbook (especificación del protocolo, pruebas con evidencias, demostración y código) | `docs/logbook/` |
| Imágenes (arquitectónico, clases, estados, secuencia exitosa y de errores, casos de uso) | `docs/diagramas/` |
| Resultados de las pruebas | `docs/pruebas/` |
| Informe ejecutivo y anexo de diagramas | `docs/informe/` |

## Alcance y limitaciones

- Se atiende un cliente a la vez; los demás esperan en la cola de conexiones del sistema.
- Los datos están precargados en memoria; no se guardan cambios ni historial de consultas.
- Sin autenticación ni cifrado: el protocolo viaja en texto plano.
- Los límites del protocolo (256 bytes, N máximo 10) están documentados en el Logbook.
- Si un cliente envía un mensaje mucho mayor que 256 bytes, el servidor responde 403 y cierra; con volúmenes muy grandes la respuesta podría llegar como reinicio de conexión (limitación documentada en el Logbook).
- No se implementó el cliente gráfico (Windows Forms) ni la atención de varios clientes a la vez.

## Autores

- Hernán Suntaxi
- Roy Tufiño

Grupo GR1B_07 · Periodo 2026B
