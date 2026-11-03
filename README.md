# AutoSalida

Programa **portable** (sin instalar) para Windows que cambia solo la salida de audio cuando apagas unos auriculares: si el dispositivo **principal** deja de estar disponible, pasa al **secundario** (por ejemplo, la barra de sonido). Cuando el principal vuelve a estar activo, lo recupera.

Gasta **0 % de CPU en reposo**: no hace comprobaciones periódicas, espera a que Windows avise de un cambio.

## Qué hace

1. Eliges el **Dispositivo de salida PRINCIPAL** (tus auriculares).
2. Eliges el **Dispositivo de salida SECUNDARIO** (la barra de sonido). Solo se usa cuando el principal está apagado, desconectado o desactivado.
3. Pulsas el botón: **ON** (verde) activa la función y **OFF** (rojo) la desactiva. Con OFF no toca ninguna salida de audio.

Las listas muestran todos los dispositivos de salida que Windows tiene detectados: primero los activos y al final los desconectados, entre corchetes.

## Cómo se usa

1. Descarga o clona el repositorio.
2. Abre `AutoSalida.exe`. No instala nada ni toca el registro.
3. Elige principal y secundario, y pulsa el botón hasta que ponga **ON**.
4. Deja la ventana abierta (puedes minimizarla). Se recuerda la configuración la próxima vez.

La configuración se guarda en un `config.ini` junto al `.exe`. Para borrar todo rastro, borra la carpeta.

Si quieres que arranque al iniciar sesión, crea un acceso directo a `AutoSalida.exe` en la carpeta que abre `shell:startup`.

## Cómo funciona

- Se suscribe a los avisos de audio de Windows (`IMMNotificationClient` de la API Core Audio).
- Cuando un dispositivo cambia de estado, se añade o se quita, espera 0,6 s para agrupar avisos seguidos y decide:
  1. Si el principal está **activo**, lo pone como salida predeterminada.
  2. Si no, y el secundario está **activo**, pone el secundario.
  3. Si ninguno está activo, no cambia nada.
- Cambia la salida predeterminada en los tres roles de Windows: consola, multimedia y comunicaciones.
- Solo se permite una copia abierta a la vez.

## Límites conocidos

- Detecta "apagado" por el **estado que Windows asigna al dispositivo** (activo, desactivado, no conectado, no presente).
- Muchos auriculares inalámbricos con receptor USB (por ejemplo, Logitech Lightspeed) siguen apareciendo como **activos** en Windows aunque los apagues, porque Windows solo ve el receptor. En ese caso AutoSalida no puede saber que están apagados y no cambia la salida.
- Los auriculares Bluetooth sí suelen pasar a "no conectado" al apagarlos.
- Probado: listado de dispositivos, cambio de la salida predeterminada y arranque de la ventana. **Pendiente** de probar con auriculares Logitech reales.
- Usa la interfaz `IPolicyConfig` de Windows, que no está documentada oficialmente, para cambiar la salida predeterminada. Es la misma que usan otras herramientas de este tipo, pero una actualización de Windows podría romperla.

## Compilar

No hace falta instalar nada: Windows incluye el compilador de C#.

```powershell
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /platform:x64 /optimize `
  /r:System.Windows.Forms.dll /r:System.Drawing.dll `
  /out:AutoSalida.exe fuente\AutoSalida.cs
```

## Estructura

| Archivo | Contenido |
|---|---|
| `AutoSalida.exe` | El programa portable |
| `fuente/AutoSalida.cs` | Código fuente (un solo archivo, C#, WinForms) |
| `LEEME.txt` | La petición original, cómo funciona y el historial de cambios |
| `config.ini` | Se crea solo al elegir dispositivos (no se sube al repositorio) |

## Historial

- **v1.1** (03/11/2026): renombrado a AutoSalida y publicación del repositorio.
- **v1.0** (03/11/2026): primera versión con principal, secundario y botón ON/OFF.
