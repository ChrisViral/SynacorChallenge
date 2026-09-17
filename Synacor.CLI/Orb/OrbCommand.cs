using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using DotMake.CommandLine;
using Microsoft.Extensions.Logging;

namespace Synacor.CLI.Orb;

/// <summary>
/// Finds the shortest valid orb path
/// </summary>
[CliCommand(Name = "orb", Description = "Finds the shortest valid orb path", Parent = typeof(SynacorCommand))]
public sealed partial class OrbCommand(ILogger<OrbCommand> logger) : ICliRunAsyncWithContextAndReturn
{
    /// <summary>
    /// Room operand
    /// </summary>
    private enum Operand
    {
        ADD = '+',
        SUB = '-',
        MUL = '*'
    }

    /// <summary>
    /// Vector struct
    /// </summary>
    /// <param name="X">X coordinate</param>
    /// <param name="Y">Y coordinate</param>
    private readonly record struct Vector(int X, int Y)
    {
        /// <summary> Up vector </summary>
        private static readonly Vector Up    = new( 0, -1);
        /// <summary> Down vector </summary>
        private static readonly Vector Down  = new( 0,  1);
        /// <summary> Left vector </summary>
        private static readonly Vector Left  = new(-1,  0);
        /// <summary> Right vector </summary>
        private static readonly Vector Right = new( 1,  0);

        /// <summary>
        /// Gets the value at the given position in the specified grid
        /// </summary>
        /// <param name="grid">Grid to get the value in</param>
        /// <returns>The value in the <paramref name="grid"/> at the given position</returns>
        public int Get(int[,] grid) => grid[this.Y, this.X];

        /// <summary>
        /// Gets all the neighbour nodes with their respective operands reachable from this position vector
        /// </summary>
        /// <returns>Enumerable of neighbour nodes and operands</returns>
        public IEnumerable<(Vector operand, Vector value)> GetNeighbours()
        {
            Vector operand = this + Up;
            yield return (operand, operand + Left);
            yield return (operand, operand + Right);
            yield return (operand, operand + Up);

            operand = this + Down;
            yield return (operand, operand + Left);
            yield return (operand, operand + Right);
            yield return (operand, operand + Down);

            operand = this + Left;
            yield return (operand, operand + Up);
            yield return (operand, operand + Down);
            yield return (operand, operand + Left);

            operand = this + Right;
            yield return (operand, operand + Up);
            yield return (operand, operand + Down);
            yield return (operand, operand + Right);
        }

        /// <summary>
        /// Vector addition
        /// </summary>
        /// <param name="left">Left vector</param>
        /// <param name="right">Right vector</param>
        /// <returns>The sum of <paramref name="left"/> and <paramref name="right"/></returns>
        public static Vector operator +(Vector left, Vector right) => new(left.X + right.X, left.Y + right.Y);
    }

    /// <summary>
    /// Search state object
    /// </summary>
    /// <param name="Position">State position</param>
    /// <param name="OrbValue">State obrb value</param>
    /// <param name="Operation">Operation string while reaching this state</param>
    /// <param name="Previous">Parent state</param>
    private sealed record State(Vector Position, int OrbValue, string Operation, State? Previous = null)
    {
        /// <inheritdoc />
        public bool Equals(State? other) => other is not null
                                         && this.Position == other.Position
                                         && this.OrbValue == other.OrbValue;

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(this.Position.GetHashCode(), this.OrbValue);
    }

    /// <summary>
    /// Logger instance
    /// </summary>
    private ILogger Logger { get; } = logger;

    /// <summary>
    /// Orb graph file, rows are set by line, columns by space separation within lines
    /// </summary>
    [CliArgument(Description = "Orb graph file, rows are set by line, columns by space separation within lines", ValidationRules = CliValidationRules.ExistingFile)]
    public required FileInfo Data { get; set; }

    /// <summary>
    /// Required door value
    /// </summary>
    [CliArgument(Description = "Required door value")]
    public int Door { get; set; }

    /// <inheritdoc />
    public async Task<int> RunAsync(CliContext context)
    {
        // Make sure the file exists
        if (!this.Data.Exists)
        {
            LogFileDoesNotExist(this.Logger, this.Data.FullName);
            return 1;
        }

        // Read file contents
        LogParsingGraph(this.Logger, this.Data.FullName);
        using StreamReader reader = this.Data.OpenText();
        string data = await reader.ReadToEndAsync(context.CancellationToken);

        // Parse grid data
        if (!TryParseGrid(data, out int[,]? grid))
        {
            LogParsingGraphFailed(this.Logger);
            return 1;
        }

        // Get grid dimensions
        int width  = grid.GetLength(0);
        int height = grid.GetLength(1);

        // Get start and end positions
        Vector startPosition = new(0, height - 1);
        Vector endPosition   = new(width - 1, 0);

        // Get start and end state
        State start = new(startPosition, startPosition.Get(grid), startPosition.Get(grid).ToString());
        State end   = new(endPosition, this.Door, string.Empty);

        // Try and find path
        if (!TryFindPath(start, end, grid, out State? state))
        {
            LogNoPathFound(this.Logger);
            return 1;
        }

        // Extract path
        Stack<string> operations = new();
        operations.Push(state.Operation);
        while(state.Previous is not null)
        {
            state = state.Previous;
            operations.Push(state.Operation);
        }

        // Log path
        string operation = string.Join(' ', operations);
        LogPathFound(this.Logger, operation, this.Door);
        return 0;
    }

    /// <summary>
    /// Tries to parse the graph grid
    /// </summary>
    /// <param name="data">File data string</param>
    /// <param name="grid">Parsed grid output</param>
    /// <returns><see langword="true"/> if the grid was successfully parsed, otherwise <see langword="false"/></returns>
    private static bool TryParseGrid(string data, [NotNullWhen(true)] out int[,]? grid)
    {

        // Figure out grid size
        string[] lines = data.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        string[]? splits = lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        int width = splits.Length;
        int height = lines.Length;
        grid = new int[width, height];

        // Parsing data from grid
        for (int y = 0; y < height; y++)
        {
            splits ??= lines[y].Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (splits.Length != width)
            {
                grid = null;
                return false;
            }

            // Parse row
            for (int x = 0; x < width; x++)
            {
                string split = splits[x];
                if (int.TryParse(split, out int value))
                {
                    grid[y, x] = value;
                }
                else
                {
                    grid[y, x] = split[0];
                }
            }
            splits = null;
        }

        return true;
    }

    /// <summary>
    /// Tries to find the shortest valid path to the door within the grid
    /// </summary>
    /// <param name="start">Start state</param>
    /// <param name="end">End state</param>
    /// <param name="grid">Graph grid</param>
    /// <param name="found">Found final state, if successful</param>
    /// <returns><see langword="true"/> if a valid path was found, otherwise <see langword="false"/></returns>
    /// <exception cref="InvalidEnumArgumentException"></exception>
    /// ReSharper disable once CognitiveComplexity
    private static bool TryFindPath(in State start, in State end, int[,] grid, [NotNullWhen(true)] out State? found)
    {
        // Setup search queue
        int width  = grid.GetLength(0);
        int height = grid.GetLength(1);
        Queue<State> search = new(100);
        search.Enqueue(start);

        // Dequeue until exhausted
        while (search.TryDequeue(out State? current))
        {
            // Go through possible neighbours
            foreach ((Vector operandPosition, Vector valuePosition) in current.Position.GetNeighbours())
            {
                // Make sure we haven't gone back to the start or out of the grid
                if (valuePosition == start.Position
                 || valuePosition.X < 0 || valuePosition.X >= width
                 || valuePosition.Y < 0 || valuePosition.Y >= height) continue;

                // Get new orb value
                Operand operand = (Operand)operandPosition.Get(grid);
                int value = valuePosition.Get(grid);
                int orbValue = operand switch
                {
                    Operand.ADD => current.OrbValue + value,
                    Operand.SUB => current.OrbValue - value,
                    Operand.MUL => current.OrbValue * value,
                    _           => throw new InvalidEnumArgumentException(nameof(operand), (int)operand, typeof(Operand))
                };

                // Make new state and check if we've reached the end
                State newState = new(valuePosition, orbValue, $"{(char)operand} {value}", current);
                if (newState == end)
                {
                    found = newState;
                    return true;
                }

                // If not, enqueue the state if not in the end room
                if (newState.Position != end.Position)
                {
                    search.Enqueue(newState);
                }
            }
        }

        // Search failed
        found = null;
        return false;
    }
}
