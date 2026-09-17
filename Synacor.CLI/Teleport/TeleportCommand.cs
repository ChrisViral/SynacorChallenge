using DotMake.CommandLine;
using Microsoft.Extensions.Logging;
using Synacor.Data;

namespace Synacor.CLI.Teleport;

/// <summary>
/// Calculates the teleport register value
/// </summary>
/// <param name="logger">Logger instance</param>
[CliCommand(Name = "teleport", Description = "Calculates the teleport register value", Parent = typeof(SynacorCommand))]
public sealed partial class TeleportCommand(ILogger<TeleportCommand> logger) : ICliRunWithReturn
{
    /// <summary>
    /// Initial A value
    /// </summary>
    [CliArgument(Description = "Initial A value")]
    public ushort A { get; set; }

    /// <summary>
    /// Initial B value
    /// </summary>
    [CliArgument(Description = "Initial B value")]
    public ushort B { get; set; }

    /// <summary>
    /// Exepcted resulting A value
    /// </summary>
    [CliArgument(Description = "Exepcted resulting A value")]
    public ushort ExpectedResult { get; set; }

    /// <summary>
    /// Logger instance
    /// </summary>
    private ILogger Logger { get; } = logger;

    /// <inheritdoc />
    public int Run()
    {
        LogFindingRegister(this.Logger, this.A, this.B, this.ExpectedResult);
        if (FindTeleport(out int register))
        {
            LogFoundRegister(this.Logger, register);
            return 0;
        }

        LogDidNotFindRegister(this.Logger);
        return 1;
    }

    /// <summary>
    /// Finds the eight register value which causes the teleport check to succeed
    /// </summary>
    /// <param name="register">Found register value</param>
    /// <returns><see langword="true"/> if a valid register value was found, otherwise false</returns>
    private bool FindTeleport(out int register)
    {
        // Math mod value
        const int MOD = Value.MAX_VALUE + 1;

        // Setup first layer
        Span<ushort> firstLayer = stackalloc ushort[Value.MAX_VALUE + 1];
        for (int n = 0; n <= Value.MAX_VALUE; n++)
        {
            // T(0, n) = n + 1
            firstLayer[n] = (ushort)((n + 1) % MOD);
        }

        // Create buffers
        Span<ushort> previous = stackalloc ushort[Value.MAX_VALUE + 1];
        Span<ushort> current  = stackalloc ushort[Value.MAX_VALUE + 1];

        // Go through values exhaustively
        for (register = 1; register <= Value.MAX_VALUE; register++)
        {
            // If the register causes the check to succeed, return with the correct register value being already set
            firstLayer.CopyTo(previous);
            if (CheckTeleport(register, previous, current))
            {
                return true;
            }
        }

        // No value found
        register = -1;
        return false;
    }

    /// <summary>
    /// Teleport check function
    /// </summary>
    /// <param name="register">Eighth register input</param>
    /// <param name="previous">Previous layer buffer</param>
    /// <param name="current">Current layer buffer</param>
    /// <returns><see langword="true"/> if the teleport check succeeded, otherwise <see langword="false"/></returns>
    private bool CheckTeleport(int register, Span<ushort> previous, Span<ushort> current)
    {
        for (int m = 1; m < this.A; m++)
        {
            // T(m, 0) = T(m - 1, register)
            current[0] = previous[register];
            for (int n = 1; n <= Value.MAX_VALUE; n++)
            {
                // T(m, n) = T(m - 1, T(m, n - 1))
                current[n] = previous[current[n - 1]];
            }

            // Swap buffers
            Span<ushort> temp = current;
            current = previous;
            previous = temp;
        }

        // Get final layer
        // T(m, 0) = T(m - 1, register)
        current[0] = previous[register];
        for (int n = 1; n <= this.B; n++)
        {
            // T(m, n) = T(m - 1, T(m, n - 1))
            current[n] = previous[current[n - 1]];
        }

        // Return out expected register
        return current[this.B] == this.ExpectedResult;
    }
}
