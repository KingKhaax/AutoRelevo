<p align="center"><img src="fuente/recursos/logo.png" alt="AutoRelevo" width="260"></p>

# AutoRelevo

Programa **portable** (sin instalar) para Windows que cambia solo la salida de audio cuando apagas unos auriculares: si el dispositivo **principal** deja de estar disponible, pasa al **secundario** (por ejemplo, la barra de sonido). Cuando el principal vuelve a estar activo, lo recupera.

Gasta **0 % de CPU en reposo**: no hace comprobaciones periódicas, espera a que Windows avise de un cambio.

## Qué hace

1. Eliges el **Dispositivo de salida PRINCIPAL** (tus auriculares).
2. Eliges el **Dispositivo de salida SECUNDARIO** (la barra de sonido). Solo se usa cuando el principal está apagado, desconectado o desactivado.
3. Pulsas el botón: **ON** (azul) activa la función y **OFF** (naranja) la desactiva. Con OFF no toca ninguna salida de audio.

Las listas muestran todos los dispositivos de salida que Windows tiene detectados: primero los activos y al final los desconectados, entre corchetes.

## Cómo se usa

1. Descarga o clona el repositorio.
2. Abre `AutoRelevo.exe`. No instala nada ni toca el registro.
3. Elige principal y secundario, y pulsa el botón hasta que ponga **ON**.
4. Deja la ventana abierta (puedes minimizarla).

AutoRelevo **recuerda los dispositivos elegidos y si lo dejaste en ON o en OFF**: al cerrarlo y volver a abrirlo queda exactamente igual. Si un dispositivo guardado ya no aparece en Windows, se conserva en la lista como "no encontrado" en vez de borrarse.

La configuración se guarda en un `config.ini` oculto junto al `.exe` (Explorador > Ver > Mostrar > Elementos ocultos para verlo). Para borrar todo rastro, borra la carpeta.

Si quieres que arranque solo al iniciar sesión, haz clic derecho en el icono junto al reloj y marca **Iniciar con Windows**. Crea una tarea programada de Windows "al iniciar sesión" (sin permisos de administrador, no toca el registro) y arranca oculto, sin ventana. Si mueves la carpeta del programa, ábrelo una vez desde la nueva ubicación y se actualiza solo.

## Cómo funciona

- Se suscribe a los avisos de audio de Windows (`IMMNotificationClient` de la API Core Audio).
- Cuando un dispositivo cambia de estado, se añade o se quita, espera 0,6 s para agrupar avisos seguidos y decide:
  1. Si el principal está **activo**, lo pone como salida predeterminada.
  2. Si no, y el secundario está **activo**, pone el secundario.
  3. Si ninguno está activo, no cambia nada.
- Cambia la salida predeterminada en los tres roles de Windows: consola, multimedia y comunicaciones.
- Al cerrar la ventana con la X el programa **no se cierra**: se oculta junto al reloj (icono de AutoRelevo) y sigue funcionando. Para volver a verlo: doble clic en el icono, clic derecho > Abrir, o abrir el `.exe` otra vez. Para cerrarlo del todo: clic derecho > Salir.
- Solo se permite una copia abierta a la vez.

## Límites conocidos

- Detecta "apagado" por el **estado que Windows asigna al dispositivo** (activo, desactivado, no conectado, no presente).
- Auriculares con receptor USB: Windows los sigue viendo **activos** aunque los apagues. Solo el **Logitech G535** está resuelto (v1.6): AutoRelevo escucha al receptor por USB y sabe cuándo se apaga o enciende el auricular. Otros modelos inalámbricos con receptor USB siguen sin detectarse.
- Los auriculares Bluetooth sí suelen pasar a "no conectado" al apagarlos.
- Probado: listado de dispositivos, cambio de la salida predeterminada, arranque de la ventana, que ON/OFF y los dispositivos se recuerdan, y la detección de apagado y encendido del Logitech G535.
- Usa la interfaz `IPolicyConfig` de Windows, que no está documentada oficialmente, para cambiar la salida predeterminada. Es la misma que usan otras herramientas de este tipo, pero una actualización de Windows podría romperla.

## Compilar

No hace falta instalar nada: Windows incluye el compilador de C#.

```powershell
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /platform:x64 /optimize `
  /win32icon:fuente\recursos\icon.ico /resource:fuente\recursos\logo_ui.png,logo.png /resource:fuente\recursos\icon.ico,icon.ico `
  /r:System.Windows.Forms.dll /r:System.Drawing.dll `
  /out:AutoRelevo.exe fuente\AutoRelevo.cs
```

## Estructura

| Archivo | Contenido |
|---|---|
| `AutoRelevo.exe` | El programa portable |
| `fuente/AutoRelevo.cs` | Código fuente (un solo archivo, C#, WinForms) |
| `fuente/recursos/` | `logo.png` (original), `logo_ui.png` (logo con fondo transparente para la ventana) e `icon.ico` (9 tamaños, de 16 a 256 px). Los dos últimos se incrustan en el `.exe` |
| `LEEME.txt` | La petición original, cómo funciona y el historial de cambios |
| `config.ini` | Se crea solo al elegir dispositivos (no se sube al repositorio) |

## Notas para otra sesión (qué se hizo en v1.6, v1.7 y v1.8 y por qué)

**Problema original:** con los auriculares Logitech G535 apagados, el programa no cambiaba a la salida secundaria. Causa: el G535 usa receptor USB (Lightspeed, VID 046D, PID 0AC4) y Windows sigue marcando sus salidas como "activas" aunque el auricular esté apagado. G HUB sí lo sabe (muestra "Inactivo"), así que el receptor envía esa información por USB.

**Cómo se resolvió (v1.6), clase `Headset` en `fuente/AutoRelevo.cs`:**
- Busca con SetupAPI la interfaz HID del receptor cuya ruta contiene `vid_046d&pid_0ac4&mi_03&col02` (protocolo HID++ de Logitech, informes largos de 20 bytes que empiezan por `11 FF`).
- La abre dos veces (una para leer, otra para escribir; compartida, no molesta a G HUB) y un hilo en segundo plano espera informes: sin sondeo, 0 % de CPU.
- Avisos que manda el receptor (determinados observando con un sniffer mientras se apagaba y encendía el auricular):
  - `11 FF 05 00 00 00 ...` = auricular **apagado** (voltaje de batería 0).
  - `11 FF 05 00 HH LL 01 ...` = auricular **encendido**: `HHLL` es el **voltaje de la batería en mV** (0x1032 = 4146 mV, 0x0FD7 = 4055 mV). Error corregido en v1.9: antes se tomaba `HH != 0x10` como apagado y, al bajar la batería de 4096 mV (hacia el 86 %), el programa creía que estaban apagados y no volvía a la salida principal. Ahora solo es apagado si `HHLL` = 0.
- Al arrancar pregunta el estado enviando `11 FF 05 0A` + ceros (20 bytes):
  - responde `11 FF 05 0A 10 xx 01 ...` si está **encendido**.
  - responde `11 FF FF 05 0A 02 ...` (error HID++) si está **apagado**.
- `Evaluate()` trata el principal como no disponible si `Headset.Off` y el nombre del dispositivo contiene "G535". Si el receptor se desconecta o no se encuentra, `Off` queda en falso y el programa se comporta como antes.
- Probado con los auriculares reales: detecta apagado y encendido, y el estado inicial al arrancar. Solo está resuelto el **G535**; otros modelos necesitarían su propio sondeo (otro PID, otro índice de característica).

**Segundo plano e icono (v1.7):**
- Cerrar con la X **oculta** la ventana y deja el programa en marcha junto al reloj (`NotifyIcon` con menú Abrir/Salir). Solo "Salir" o el apagado de Windows cierran de verdad (`exiting` y `CloseReason.WindowsShutDown` en `OnFormClosing`).
- Una segunda copia no se abre: avisa a la primera con un `EventWaitHandle` (`AutoRelevo_mostrar`) y esta muestra su ventana.
- Icono de la ventana: el `.ico` completo; icono del área de notificación: tamaño pequeño del sistema. El usuario se quejaba de que la barra de tareas salía sin icono; el usuario confirmó que ya sale bien.

**Arranque con Windows (v1.8):**
- Clic derecho en el icono junto al reloj > "Iniciar con Windows" (casilla). Desde la v1.10 crea o borra una **tarea programada** de Windows llamada `AutoRelevo` (disparador "al iniciar sesión" del usuario, sin permisos de administrador, creada con `Register-ScheduledTask` lanzado desde `powershell.exe` oculto). Clase `Autostart` en `fuente/AutoRelevo.cs`.
- Por qué no el acceso directo en `shell:startup` (v1.8-v1.9): Windows retrasa las apps de esa carpeta hasta que el PC está tranquilo; en el PC del usuario AutoRelevo arrancó 2 min 20 s después de cargar el escritorio. `schtasks /create /sc onlogon` da "Acceso denegado" sin administrador, pero `Register-ScheduledTask -AtLogOn -User <usuario>` sí funciona. Al activar, también se borra el `.lnk` antiguo.
- Con `/oculto` el programa arranca directamente en segundo plano, sin ventana. Cada vez que arranca con la tarea activa, la reescribe en un hilo aparte con la ruta actual del `.exe`. Ojo: no poner barras invertidas dentro de las cadenas de C# que van a PowerShell (se rompen); por eso el usuario se obtiene con `WindowsIdentity.GetCurrent().Name`.
- El usuario pidió que arrancase solo con Windows y se dejó activado en su escritorio (`C:\Users\Usuario\Desktop\AutoRelevo.exe`).

**Trampas encontradas (para no repetirlas):**
- En Git Bash, las rutas que empiezan por `\\?\` se rompen (error 123); lanzar esos programas desde PowerShell. Lo mismo con las opciones `/xxx` del compilador: usar PowerShell para compilar.
- `Process.CloseMainWindow()` llega a la ventana como `CloseReason.TaskManagerClosing`, no como `UserClosing`. Por eso no se filtra solo por `UserClosing`.
- Los archivos del repositorio van con saltos de línea CRLF (el `.cs` además con BOM). Python no está instalado: editar con las herramientas de edición o `sed`.
- No dejar que se cuele la carpeta `.omc/` en un commit (ya pasó una vez y se quitó).
- Para sustituir el `.exe` hay que cerrar antes el proceso en marcha (`AutoRelevo.exe` queda activo en segundo plano).

**Flujo para cambios:** editar `fuente/AutoRelevo.cs` (sin comentarios en el código), compilar con el comando de arriba, probar sin abrir ventanas de más, actualizar este README y `LEEME.txt` (historial incluido), commit con la identidad sr.kitkat (sin mención a Claude) y subir a GitHub. Copiar el `.exe` nuevo al escritorio del usuario.

## Historial

- **v1.11** (07/11/2026): `config.ini` se crea y se guarda **oculto** (atributo de archivo oculto), para que no se vea en el escritorio. Para verlo: Explorador > Ver > Mostrar > Elementos ocultos.
- **v1.10** (06/11/2026): el arranque con Windows usa una tarea programada al iniciar sesión en lugar del acceso directo de la carpeta de inicio, que Windows retrasaba más de 2 minutos.
- **v1.9** (04/11/2026): corregido que, con la batería del G535 por debajo de ~4,1 V (≈ 86 %), el programa creyera los auriculares apagados y no volviera a la salida principal al encenderlos.
- **v1.8** (03/11/2026): arranque con Windows desde el menú del icono (acceso directo en la carpeta de inicio, arranca oculto) y notas técnicas para otras sesiones.
- **v1.7** (03/11/2026): al cerrar con la X sigue funcionando en segundo plano (icono junto al reloj con menú Abrir y Salir) e icono propio en la barra de tareas.
- **v1.6** (03/11/2026): soporte del Logitech G535: detecta por USB cuándo se apaga o enciende el auricular y cambia al secundario.
- **v1.3** (03/11/2026): icono rehecho solo con el emblema circular, con bordes suaves y nitidez en los tamaños pequeños (16 a 256 px), e icono incrustado en el `.exe` para la barra de título. Logo de la ventana con fondo transparente.
- **v1.2** (03/11/2026): renombrado a **AutoRelevo**, logo e icono, colores del logo (azul y naranja) y se guardan siempre los dispositivos y el estado ON/OFF.
- **v1.1** (03/11/2026): publicación del repositorio.
- **v1.0** (03/11/2026): primera versión con principal, secundario y botón ON/OFF.
