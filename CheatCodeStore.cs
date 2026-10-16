using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Mastermind;

public enum CheatCodeStatus
{
    Consumed,
    Invalid,
    NoUses
}

public sealed class CheatCodeEntry
{
    public string Hash { get; set; } = "";
    public int UsosDisponibles { get; set; }
}

public sealed class CheatCodeStore(string path)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly object fileLock = new();

    public CheatCodeStatus TryConsume(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || !File.Exists(path))
        {
            return CheatCodeStatus.Invalid;
        }

        lock (fileLock)
        {
            List<CheatCodeEntry> entries = JsonSerializer.Deserialize<List<CheatCodeEntry>>(File.ReadAllText(path), JsonOptions)
                ?? throw new InvalidDataException("El archivo de cheat codes no contiene una lista válida.");
            string hash = CreateHash(code);
            CheatCodeEntry? entry = entries.FirstOrDefault(item =>
                string.Equals(item.Hash, hash, StringComparison.OrdinalIgnoreCase));
            if (entry is null)
            {
                return CheatCodeStatus.Invalid;
            }

            if (entry.UsosDisponibles <= 0)
            {
                return CheatCodeStatus.NoUses;
            }

            entry.UsosDisponibles--;
            string? directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, JsonSerializer.Serialize(entries, JsonOptions));
            return CheatCodeStatus.Consumed;
        }
    }

    public static string CreateHash(string code)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code.Trim().ToUpperInvariant()));
        return Convert.ToHexString(bytes);
    }
}