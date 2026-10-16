using Xunit;

namespace Mastermind.Tests;

public sealed class EvaluatorTests
{
    [Fact]
    public void EvaluarIntento_CuentaCoincidenciasExactasYDesordenadas()
    {
        EvaluationResult result = MastermindEvaluator.EvaluarIntento("RGBY", "RBYG");

        Assert.Equal(1, result.BienPosicionados);
        Assert.Equal(3, result.ColorCorrecto);
    }

    [Fact]
    public void EvaluarIntento_CoincidenciaCompleta()
    {
        EvaluationResult result = MastermindEvaluator.EvaluarIntento("RGBY", "RGBY");

        Assert.Equal(4, result.BienPosicionados);
        Assert.Equal(0, result.ColorCorrecto);
    }

    [Fact]
    public void EvaluarIntento_SinColoresCoincidentes()
    {
        EvaluationResult result = MastermindEvaluator.EvaluarIntento("RGBY", "ZZZZ");

        Assert.Equal(0, result.BienPosicionados);
        Assert.Equal(0, result.ColorCorrecto);
    }

    [Fact]
    public void EvaluarIntento_DuplicadosNoSeCuentanDosVeces()
    {
        EvaluationResult result = MastermindEvaluator.EvaluarIntento("AABC", "ADAA");

        Assert.Equal(1, result.BienPosicionados);
        Assert.Equal(1, result.ColorCorrecto);
    }

    [Theory]
    [InlineData("ABCDE", "ABCDE")]
    [InlineData("RGBY", "RGB")]
    [InlineData("RGBY", "RG Y")]
    public void EvaluarIntento_RechazaEntradasInvalidas(string secret, string guess)
    {
        Assert.Throws<ArgumentException>(() => MastermindEvaluator.EvaluarIntento(secret, guess));
    }

}