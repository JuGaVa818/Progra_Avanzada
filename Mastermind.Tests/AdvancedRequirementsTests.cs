using System.Text.Json;
using Xunit;

namespace Mastermind.Tests;

public sealed class AdvancedRequirementsTests
{
    [Fact]
    public void CheatValido_RevelaUnaPosicionYReduceUsosDisponibles()
    {
        using var files = new TemporaryFiles();
        string cheatPath = CreateCheatFile(files.Path, "AYUDA", 2);
        var engine = CreateEngine(files.Path, cheatPath, "RGBY");
        SetAttempt(engine, "CPKW");

        bool submitted = engine.TrySubmit(out GuessResult? result, out string message, "AYUDA");

        Assert.True(submitted);
        Assert.NotNull(result?.Evaluation.CheatHint);
        Assert.False(result!.Evaluation.CheatHint!.SinSuerte);
        Assert.Contains("Pista: posición", message);
        Assert.Equal(1, ReadUses(cheatPath));
    }

    [Fact]
    public void CheatInutil_SeConsumeYNotificaSinSuerte()
    {
        using var files = new TemporaryFiles();
        string cheatPath = CreateCheatFile(files.Path, "AYUDA", 1);
        var engine = CreateEngine(files.Path, cheatPath, "RGBY");
        SetAttempt(engine, "RGBY");

        bool submitted = engine.TrySubmit(out GuessResult? result, out string message, "AYUDA");

        Assert.True(submitted);
        Assert.True(result!.Evaluation.CheatHint!.SinSuerte);
        Assert.Contains("sin suerte", message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, ReadUses(cheatPath));
    }

    [Fact]
    public void CheatRepetidoEnPartida_NoVuelveAConsumirElJson()
    {
        using var files = new TemporaryFiles();
        string cheatPath = CreateCheatFile(files.Path, "AYUDA", 3);
        var engine = CreateEngine(files.Path, cheatPath, "RGBY");
        SetAttempt(engine, "CPKW");
        Assert.True(engine.TrySubmit(out _, out _, "AYUDA"));

        SetAttempt(engine, "CPKW");
        bool submitted = engine.TrySubmit(out _, out string message, "AYUDA");

        Assert.False(submitted);
        Assert.Contains("ya fue utilizado", message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, ReadUses(cheatPath));
        Assert.Single(engine.History);
    }

    [Fact]
    public void CheatInvalido_NoConsumeIntentoNiUso()
    {
        using var files = new TemporaryFiles();
        string cheatPath = CreateCheatFile(files.Path, "AYUDA", 2);
        var engine = CreateEngine(files.Path, cheatPath, "RGBY");
        SetAttempt(engine, "CPKW");

        bool submitted = engine.TrySubmit(out _, out string message, "INCORRECTO");

        Assert.False(submitted);
        Assert.Contains("no es válido", message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(engine.History);
        Assert.Equal(2, ReadUses(cheatPath));
    }

    [Fact]
    public void JuegoDeSeisPosiciones_AceptaSeisColoresDistintos()
    {
        using var files = new TemporaryFiles();
        string cheatPath = CreateCheatFile(files.Path, "AYUDA", 1);
        GameEngine engine = CreateEngine(files.Path, cheatPath, "RGBYCK", 6);

        Assert.Equal(6, engine.CodeLength);
        for (int index = 0; index < engine.CodeLength; index++)
        {
            Assert.True(engine.TrySetColor(index, "RGBYCK"[index]));
        }

        Assert.True(engine.TrySubmit(out GuessResult? result, out _));
        Assert.Equal(6, result!.Evaluation.BienPosicionados);
    }

    [Fact]
    public void Juego_RechazaColorRepetidoEnElIntento()
    {
        using var files = new TemporaryFiles();
        string cheatPath = CreateCheatFile(files.Path, "AYUDA", 1);
        GameEngine engine = CreateEngine(files.Path, cheatPath, "RGBY");

        Assert.True(engine.TrySetColor(0, 'R'));
        Assert.False(engine.TrySetColor(1, 'R'));
    }

    [Fact]
    public void ConfiguracionCargaColoresYTamanosDesdeJson()
    {
        using var files = new TemporaryFiles();
        string path = System.IO.Path.Combine(files.Path, "game-config.json");
        File.WriteAllText(path, JsonSerializer.Serialize(new GameConfiguration { TamanoInicial = 6 }));

        GameConfiguration configuration = GameConfiguration.LoadOrCreate(path);

        Assert.Equal(6, configuration.TamanoInicial);
        Assert.Contains(4, configuration.TamanosPermitidos);
        Assert.Contains(6, configuration.TamanosPermitidos);
        Assert.Equal(10, configuration.Colores.Count);
    }

    [Fact]
    public void Logger_RegistraFechaPatronIntentoYResultado()
    {
        using var files = new TemporaryFiles();
        string logPath = System.IO.Path.Combine(files.Path, "attempts.log");
        var logger = new FileAttemptLogger(logPath);

        logger.Log("RGBY", "RBYG", new EvaluationResult(1, 3));

        string line = File.ReadAllText(logPath);
        Assert.Contains("se evaluó el intento RBYG contra el patrón RGBY", line);
        Assert.Contains("Resultado: 1 bien posicionados – 3 con color correcto.", line);
        Assert.Matches(@"\d{2}-\d{2}-\d{4} \d{2}:\d{2}:\d{2}", line);
    }

    [Fact]
    public void Evaluador_CheatEnPosicionCorrectaIndicaSinSuerte()
    {
        EvaluationResult result = MastermindEvaluator.EvaluarIntento("RGBY", "RGBY", "TEST-CODE", new Random(1));

        Assert.NotNull(result.CheatHint);
        Assert.True(result.CheatHint.SinSuerte);
        Assert.Null(result.CheatHint.Color);
    }

    [Fact]
    public void Evaluador_CheatRevelaLaPosicionSeleccionadaAlAzar()
    {
        EvaluationResult result = MastermindEvaluator.EvaluarIntento("RGBY", "ZZZZ", "TEST-CODE", new Random(1));

        Assert.NotNull(result.CheatHint);
        Assert.False(result.CheatHint.SinSuerte);
        Assert.Equal("RGBY"[result.CheatHint.Position - 1], result.CheatHint.Color);
    }

    private static GameEngine CreateEngine(string directory, string cheatPath, string secret, int codeLength = 4)
    {
        GameConfiguration configuration = GameConfiguration.LoadOrCreate(System.IO.Path.Combine(directory, "game-config.json"));
        return new GameEngine(
            configuration,
            new CheatCodeStore(cheatPath),
            new FileAttemptLogger(System.IO.Path.Combine(directory, "attempts.log")),
            codeLength,
            secret);
    }

    private static string CreateCheatFile(string directory, string code, int uses)
    {
        string path = System.IO.Path.Combine(directory, "cheat-codes.json");
        var entries = new[]
        {
            new CheatCodeEntry { Hash = CheatCodeStore.CreateHash(code), UsosDisponibles = uses }
        };
        File.WriteAllText(path, JsonSerializer.Serialize(entries));
        return path;
    }

    private static int ReadUses(string path) =>
        JsonSerializer.Deserialize<List<CheatCodeEntry>>(
            File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })![0].UsosDisponibles;

    private static void SetAttempt(GameEngine engine, string attempt)
    {
        for (int index = 0; index < attempt.Length; index++)
        {
            Assert.True(engine.TrySetColor(index, attempt[index]));
        }
    }

    private sealed class TemporaryFiles : IDisposable
    {
        public TemporaryFiles()
        {
            Directory.CreateDirectory(Path);
        }

        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}