using System.Globalization;
using System.Text;

namespace Mastermind;

public interface IAttemptLogger
{
    void Log(string patronSecreto, string intentoJugador, EvaluationResult resultado);
}

public sealed class FileAttemptLogger(string path) : IAttemptLogger
{
    private readonly object fileLock = new();

    public void Log(string patronSecreto, string intentoJugador, EvaluationResult resultado)
    {
        string timestamp = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture);
        string line = $"En la fecha {timestamp} se evaluó el intento {intentoJugador} contra el patrón {patronSecreto}. " +
            $"Resultado: {resultado.BienPosicionados} bien posicionados – {resultado.ColorCorrecto} con color correcto.{Environment.NewLine}";
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        lock (fileLock)
        {
            File.AppendAllText(path, line, Encoding.UTF8);
        }
    }
}