using System.Text.Json;

namespace Mastermind;

public sealed class ColorDefinition
{
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Hex { get; set; } = "";
}

public sealed class GameConfiguration
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public int TamanoInicial { get; set; } = 4;
    public List<int> TamanosPermitidos { get; set; } = [4, 6];
    public List<ColorDefinition> Colores { get; set; } = CreateDefaultColors();

    public static GameConfiguration LoadOrCreate(string path)
    {
        if (!File.Exists(path))
        {
            string? directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, JsonSerializer.Serialize(new GameConfiguration(), JsonOptions));
        }

        GameConfiguration configuration = JsonSerializer.Deserialize<GameConfiguration>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidDataException("No se pudo leer la configuración del juego.");
        configuration.Validate();
        return configuration;
    }

    public void Validate()
    {
        if (TamanosPermitidos is null || TamanosPermitidos.Count == 0 ||
            TamanosPermitidos.Any(size => size is not (4 or 6)) ||
            TamanosPermitidos.Distinct().Count() != TamanosPermitidos.Count ||
            !TamanosPermitidos.Contains(TamanoInicial))
        {
            throw new InvalidDataException("Los tamaños permitidos deben ser 4 o 6 e incluir el tamaño inicial.");
        }

        if (Colores is null || Colores.Count < TamanosPermitidos.Max())
        {
            throw new InvalidDataException("La paleta debe tener al menos tantos colores como el mayor tamaño permitido.");
        }

        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (ColorDefinition color in Colores)
        {
            if (color.Codigo.Length != 1 || !char.IsAsciiLetterOrDigit(color.Codigo[0]) ||
                string.IsNullOrWhiteSpace(color.Nombre) || !IsHexColor(color.Hex) || !codes.Add(color.Codigo))
            {
                throw new InvalidDataException("Cada color necesita un código único de una letra o dígito, nombre y HEX #RRGGBB válido.");
            }
        }
    }

    public bool ContainsColor(char code) => Colores.Any(color =>
        string.Equals(color.Codigo, code.ToString(), StringComparison.OrdinalIgnoreCase));

    public ColorDefinition GetColor(char code) => Colores.First(color =>
        string.Equals(color.Codigo, code.ToString(), StringComparison.OrdinalIgnoreCase));

    private static bool IsHexColor(string value)
    {
        if (value.Length != 7 || value[0] != '#')
        {
            return false;
        }

        return value.AsSpan(1).ToArray().All(Uri.IsHexDigit);
    }

    private static List<ColorDefinition> CreateDefaultColors() =>
    [
        new() { Codigo = "R", Nombre = "Red", Hex = "#F46A5C" },
        new() { Codigo = "G", Nombre = "Green", Hex = "#5BCC8B" },
        new() { Codigo = "B", Nombre = "Blue", Hex = "#52A4E1" },
        new() { Codigo = "Y", Nombre = "Yellow", Hex = "#F6C64B" },
        new() { Codigo = "C", Nombre = "Cyan", Hex = "#55CFD2" },
        new() { Codigo = "K", Nombre = "Black", Hex = "#41484C" },
        new() { Codigo = "P", Nombre = "Purple", Hex = "#AE82DC" },
        new() { Codigo = "W", Nombre = "White", Hex = "#E7EAE1" },
        new() { Codigo = "I", Nombre = "Pink", Hex = "#F48FB1" },
        new() { Codigo = "N", Nombre = "Brown", Hex = "#9B6843" }
    ];
}