namespace Mastermind;

public sealed record CheatHint(int Position, char? Color, bool SinSuerte);

public sealed record EvaluationResult(int BienPosicionados, int ColorCorrecto, CheatHint? CheatHint = null);

public static class MastermindEvaluator
{
    public static EvaluationResult EvaluarIntento(
        string patronSecreto,
        string intentoJugador,
        string? cheatCode = null,
        Random? random = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(patronSecreto);
        ArgumentException.ThrowIfNullOrWhiteSpace(intentoJugador);

        string secret = patronSecreto.ToUpperInvariant();
        string guess = intentoJugador.ToUpperInvariant();
        if (secret.Length is not (4 or 6) || guess.Length != secret.Length)
        {
            throw new ArgumentException("El patrón y el intento deben tener el mismo tamaño: 4 o 6.");
        }

        if (secret.Any(color => !char.IsAsciiLetterOrDigit(color)) ||
            guess.Any(color => !char.IsAsciiLetterOrDigit(color)))
        {
            throw new ArgumentException("Los colores deben representarse con letras o números, sin espacios.");
        }

        int exact = 0;
        var remainingSecretColors = new Dictionary<char, int>();
        for (int index = 0; index < secret.Length; index++)
        {
            if (secret[index] == guess[index])
            {
                exact++;
                continue;
            }

            remainingSecretColors.TryGetValue(secret[index], out int count);
            remainingSecretColors[secret[index]] = count + 1;
        }

        int misplaced = 0;
        for (int index = 0; index < guess.Length; index++)
        {
            if (secret[index] == guess[index] || !remainingSecretColors.TryGetValue(guess[index], out int count))
            {
                continue;
            }

            misplaced++;
            if (count == 1)
            {
                remainingSecretColors.Remove(guess[index]);
            }
            else
            {
                remainingSecretColors[guess[index]] = count - 1;
            }
        }

        CheatHint? hint = null;
        if (!string.IsNullOrWhiteSpace(cheatCode))
        {
            int position = (random ?? Random.Shared).Next(secret.Length);
            bool alreadyCorrect = secret[position] == guess[position];
            hint = new CheatHint(position + 1, alreadyCorrect ? null : secret[position], alreadyCorrect);
        }

        return new EvaluationResult(exact, misplaced, hint);
    }
}