using DotMake.CommandLine;
using Microsoft.Extensions.Logging;

namespace Synacor.CLI;

/// <summary>
/// Coin problem calculation command <br/>
/// c_1 + c_2 * c_3^2 + c_4^3 - c_5 = result
/// </summary>
[CliCommand(Name = "coins", Description = "Coin problem calculation command\nc_1 + c_2 * c_3^2 + c_4^3 - c_5 = result", Parent = typeof(SynacorCommand))]
public sealed partial class CoinsCommand(ILogger<CoinsCommand> logger) : ICliRunWithReturn
{
    /// <summary>
    /// Logger instance
    /// </summary>
    private ILogger Logger { get; } = logger;

    /// <summary>
    /// First coin value
    /// </summary>
    [CliArgument(Description = "First coin value")]
    public int A { get; set; }

    /// <summary>
    /// Second coin value
    /// </summary>
    [CliArgument(Description = "Second coin value")]
    public int B { get; set; }

    /// <summary>
    /// Third coin value
    /// </summary>
    [CliArgument(Description = "Third coin value")]
    public int C { get; set; }

    /// <summary>
    /// Fourth coin value
    /// </summary>
    [CliArgument(Description = "Fourth coin value")]
    public int D { get; set; }

    /// <summary>
    /// Fifth coin value
    /// </summary>
    [CliArgument(Description = "Fifth coin value")]
    public int E { get; set; }

    /// <summary>
    /// Expected result value
    /// </summary>
    [CliArgument(Description = "Expected result value")]
    public int Result { get; set; }

    /// <inheritdoc />
    public int Run()
    {
        // Log info
        LogSolving(this.Logger);
        LogCoins(this.Logger, this.A, this.B, this.C, this.D, this.E);
        LogFormula(this.Logger, this.Result);

        // Try and solve
        Span<int> coins = [this.A, this.B, this.C, this.D, this.E];
        if (Solve(coins) && coins is [int a, int b, int c ,int d, int e])
        {
            // Log solution
            LogSolutionFound(this.Logger, a, b, c, d, e, this.Result);
            return 0;
        }

        // Notify failure
        LogNoSolution(this.Logger);
        return 1;
    }

    /// <summary>
    /// Tries to solve the formula for the given coins and result, in case of success, the <paramref name="coins"/> span will be modified in-place
    /// </summary>
    /// <param name="coins">Coins to order</param>
    /// <returns><see langword="true"/> if a solution was found, otherwise <see langword="false"/></returns>
    // ReSharper disable once CognitiveComplexity
    private bool Solve(Span<int> coins)
    {
        if (IsSatisfied(coins)) return true;

        Span<int> counters = stackalloc int[coins.Length];
        for (int i = 0; i < coins.Length;)
        {
            if (counters[i] < i)
            {
                if (int.IsEvenInteger(i))
                {
                    (coins[0], coins[i]) = (coins[i], coins[0]);
                }
                else
                {
                    (coins[counters[i]], coins[i]) = (coins[i], coins[counters[i]]);
                }

                if (IsSatisfied(coins)) return true;
                counters[i]++;
                i = 0;
            }
            else
            {
                counters[i] = 0;
                i++;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the coins in this order satisfy the formula
    /// </summary>
    /// <param name="coins">Ordered coins</param>
    /// <returns><see langword="true"/> if the formula is satisfied, otherswise <see langword="false"/></returns>
    private bool IsSatisfied(Span<int> coins)
    {
        if (coins is [int a, int b, int c, int d, int e])
        {
            int result = a + (b * c * c) + (d * d * d) - e;
            return result == this.Result;
        }

        return false;
    }
}
