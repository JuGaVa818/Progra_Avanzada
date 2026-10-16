namespace Mastermind;

public sealed record GuessResult(string Attempt, EvaluationResult Evaluation);

public sealed class GameEngine
{
    public const int MaximumAttempts = 10;

    private readonly CheatCodeStore cheatCodeStore;
    private readonly IAttemptLogger attemptLogger;
    private readonly string secret;
    private readonly List<GuessResult> history = [];
    private readonly HashSet<string> usedCheatCodes = new(StringComparer.OrdinalIgnoreCase);
    private bool cheatUsedThisGame;

    public GameEngine(
        GameConfiguration configuration,
        CheatCodeStore cheatCodeStore,
        IAttemptLogger attemptLogger,
        int? codeLength = null,
        string? patronSecreto = null,
        string? previousSecret = null)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        Configuration.Validate();
        this.cheatCodeStore = cheatCodeStore ?? throw new ArgumentNullException(nameof(cheatCodeStore));
        this.attemptLogger = attemptLogger ?? throw new ArgumentNullException(nameof(attemptLogger));
        CodeLength = codeLength ?? configuration.TamanoInicial;
        if (!configuration.TamanosPermitidos.Contains(CodeLength))
        {
            throw new ArgumentOutOfRangeException(nameof(codeLength), "El tamaño debe estar permitido por la configuración.");
        }

        secret = patronSecreto is null ? GenerateSecret(previousSecret) : ValidateSecret(patronSecreto);
        CurrentGuess = new char?[CodeLength];
    }

    public GameConfiguration Configuration { get; }
    public int CodeLength { get; }
    public char?[] CurrentGuess { get; }
    public IReadOnlyList<GuessResult> History => history;
    public bool IsWon { get; private set; }
    public bool IsOver => IsWon || history.Count >= MaximumAttempts;
    public int AttemptsRemaining => MaximumAttempts - history.Count;

    public GameEngine CreateNextGame(int? codeLength = null) =>
        new(Configuration, cheatCodeStore, attemptLogger, codeLength ?? CodeLength, previousSecret: secret);

    public bool TrySetColor(int position, char color)
    {
        char normalized = char.ToUpperInvariant(color);
        if (IsOver || position < 0 || position >= CodeLength || !Configuration.ContainsColor(normalized))
        {
            return false;
        }

        if (CurrentGuess.Where((_, index) => index != position).Contains(normalized))
        {
            return false;
        }

        CurrentGuess[position] = normalized;
        return true;
    }

    public void ClearColor(int position)
    {
        if (!IsOver && position >= 0 && position < CodeLength)
        {
            CurrentGuess[position] = null;
        }
    }

    public bool TrySubmit(out GuessResult? result, out string message, string? cheatCode = null)
    {
        result = null;
        if (IsOver)
        {
            message = "La partida ha terminado. Inicia una nueva para seguir jugando.";
            return false;
        }

        if (CurrentGuess.Any(color => color is null))
        {
            message = $"Completa los {CodeLength} espacios antes de probar.";
            return false;
        }

        string attempt = new(CurrentGuess.Select(color => color!.Value).ToArray());
        string? normalizedCheat = null;
        if (!string.IsNullOrWhiteSpace(cheatCode))
        {
            normalizedCheat = cheatCode.Trim();
            if (cheatUsedThisGame)
            {
                message = usedCheatCodes.Contains(normalizedCheat)
                    ? "Este cheat code ya fue utilizado en esta partida."
                    : "Solo se permite un cheat code por partida.";
                return false;
            }

            CheatCodeStatus cheatStatus = cheatCodeStore.TryConsume(normalizedCheat);
            if (cheatStatus != CheatCodeStatus.Consumed)
            {
                message = cheatStatus == CheatCodeStatus.NoUses
                    ? "Ese cheat code ya no tiene usos disponibles."
                    : "El cheat code no es válido.";
                return false;
            }

            cheatUsedThisGame = true;
            usedCheatCodes.Add(normalizedCheat);
        }

        EvaluationResult evaluation = MastermindEvaluator.EvaluarIntento(secret, attempt, normalizedCheat);
        result = new GuessResult(attempt, evaluation);
        history.Add(result);
        IsWon = evaluation.BienPosicionados == CodeLength;
        Array.Clear(CurrentGuess);
        attemptLogger.Log(secret, attempt, evaluation);

        string scoreMessage = $"{evaluation.BienPosicionados} bien posicionados · {evaluation.ColorCorrecto} con color correcto.";
        if (evaluation.CheatHint is { } hint)
        {
            message = hint.SinSuerte
                ? $"{scoreMessage} Cheat code consumido: sin suerte."
                : $"{scoreMessage} Pista: posición {hint.Position} = {hint.Color}. Cheat code consumido.";
        }
        else
        {
            message = IsWon
                ? "¡Código descifrado! Puedes iniciar otra partida."
                : IsOver
                    ? "Se acabaron los intentos. Inicia otra partida para volver a jugar."
                    : scoreMessage;
        }

        return true;
    }

    private string ValidateSecret(string value)
    {
        string normalized = value.ToUpperInvariant();
        if (normalized.Length != CodeLength || normalized.Any(color => !Configuration.ContainsColor(color)))
        {
            throw new ArgumentException("El patrón secreto debe usar colores de la configuración y el tamaño seleccionado.", nameof(value));
        }

        if (normalized.Distinct().Count() != normalized.Length)
        {
            throw new ArgumentException("El patrón secreto no puede repetir colores.", nameof(value));
        }

        return normalized;
    }

    private string GenerateSecret(string? previousSecret)
    {
        string[] colors = Configuration.Colores.Select(color => color.Codigo.ToUpperInvariant()).ToArray();
        string generated;
        do
        {
            for (int index = colors.Length - 1; index > 0; index--)
            {
                int swapIndex = Random.Shared.Next(index + 1);
                (colors[index], colors[swapIndex]) = (colors[swapIndex], colors[index]);
            }

            generated = string.Concat(colors.Take(CodeLength));
        }
        while (string.Equals(generated, previousSecret, StringComparison.Ordinal));

        return generated;
    }
}