# Mastermind V2

Juego de escritorio en C# con Windows Forms para descubrir un patrón secreto. La evaluación está separada de la interfaz y cuenta primero coincidencias exactas, luego colores correctos en posición incorrecta, sin contar colores duplicados dos veces.

## Requisitos

- Windows
- .NET 8 SDK

## Ejecutar

Desde la carpeta del proyecto:

```powershell
dotnet run
```

Para ejecutar las pruebas unitarias:

```powershell
dotnet test Mastermind.Tests\Mastermind.Tests.csproj
```

## Cómo jugar

1. Elige un tamaño de código de 4 o 6 posiciones.
2. Selecciona un espacio y asigna un color de la paleta. No se puede repetir un color en el intento.
3. Pulsa **Probar**. Los indicadores verdes son aciertos exactos; los amarillos, colores correctos en otra posición.
4. Opcionalmente, introduce un cheat code antes de probar. Un código válido se consume incluso cuando la posición aleatoria ya estaba acertada y el resultado es «sin suerte».

El código secreto se genera al azar, usa colores distintos y no repite el orden de la partida anterior. Se permiten hasta diez intentos.

## Cheat codes

`cheat-codes.json` contiene hashes SHA-256 y el número de usos disponibles. Cada código puede usarse una vez por partida; los usos globales restantes se guardan en el JSON junto al ejecutable. Códigos iniciales de demostración:

- `MASTERMIND` (1 uso)
- `AYUDA` (3 usos)

El cheat revela una posición aleatoria. Si esa posición ya era correcta, muestra «sin suerte»; el uso igualmente se descuenta.

## Configuración y registro

`game-config.json` define los tamaños disponibles y la paleta. Cada color tiene un código único de una letra o dígito, un nombre y un color HEX. La configuración se valida al iniciar; se puede editar el archivo junto al ejecutable y reiniciar el juego.

Cada intento evaluado se agrega a `attempts.log` con fecha, patrón, intento y resultado. El archivo se guarda junto al ejecutable.

## Pruebas

Las pruebas cubren coincidencia completa y parcial, entradas inválidas, duplicados, cheats válidos/repetidos/inútiles, tamaños dinámicos y logging.