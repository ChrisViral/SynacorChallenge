namespace Synacor.CLI;

/// <summary>
/// <see cref="Console"/> IO provider
/// </summary>
public sealed class ConsoleProvider : IInputProvider, IOutputProvider
{
    /// <inheritdoc />
    public string ReadLine() => Console.ReadLine()!;

    /// <inheritdoc />
    public void Write(char value) => Console.Write(value);
}
