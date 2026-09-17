using Microsoft.Extensions.Logging;

namespace Synacor.CLI.Coins;

public partial class CoinsCommand
{
    [LoggerMessage(LogLevel.Information, "Solving coin problem...")]
    static partial void LogSolving(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Coins: {A}, {B}, {C}, {D}, {E}")]
    static partial void LogCoins(ILogger logger, int a, int b, int c, int d, int e);

    [LoggerMessage(LogLevel.Information, "Formula to satisfy: a + b * c^2 + d^3 - e = {Result}")]
    static partial void LogFormula(ILogger logger, int result);

    [LoggerMessage(LogLevel.Information, "Solution found: {A} + {B} * {C}^2 + {D}^3 - {E} = {Result}")]
    static partial void LogSolutionFound(ILogger logger, int a, int b, int c, int d, int e, int result);

    [LoggerMessage(LogLevel.Error, "Could not find valid integer solution for the given coins and result")]
    static partial void LogNoSolution(ILogger logger);
}
