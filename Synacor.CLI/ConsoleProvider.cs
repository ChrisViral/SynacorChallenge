namespace Synacor.CLI;

/// <summary>
/// <see cref="Console"/> IO provider
/// </summary>
public sealed class ConsoleProvider : IInputProvider, IOutputProvider
{
    private static readonly char[] ReadBuffer = new char[1];

    /// <inheritdoc />
    public async Task<char> Read(CancellationToken token = default)
    {
        await Console.In.ReadAsync(ReadBuffer, token);
        return ReadBuffer[0];
    }

    /// <inheritdoc />
    public async Task Write(char value, CancellationToken token = default) => await Console.Out.WriteAsync(value);
}
