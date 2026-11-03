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

La configuración se guarda en un `config.ini` junto al `.exe`. Para borrar todo rastro, borra la carpeta.

Si quieres que arranque al iniciar sesión, crea un acceso directo a `AutoRelevo.exe` en la carpeta que abre `shell:startup`.

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

## Historial

- **v1.7** (03/11/2026): al cerrar con la X sigue funcionando en segundo plano (icono junto al reloj con menú Abrir y Salir) e icono propio en la barra de tareas.
- **v1.6** (03/11/2026): soporte del Logitech G535: detecta por USB cuándo se apaga o enciende el auricular y cambia al secundario.
- **v1.3** (03/11/2026): icono rehecho solo con el emblema circular, con bordes suaves y nitidez en los tamaños pequeños (16 a 256 px), e icono incrustado en el `.exe` para la barra de título. Logo de la ventana con fondo transparente.
- **v1.2** (03/11/2026): renombrado a **AutoRelevo**, logo e icono, colores del logo (azul y naranja) y se guardan siempre los dispositivos y el estado ON/OFF.
- **v1.1** (03/11/2026): publicación del repositorio.
- **v1.0** (03/11/2026): primera versión con principal, secundario y botón ON/OFF.
