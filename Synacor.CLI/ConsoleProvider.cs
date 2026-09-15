namespace Synacor.CLI;

/// <summary>
/// <see cref="Console"/> IO provider
/// </summary>
public sealed class ConsoleProvider : IInputProvider, IOutputProvider
{
    /// <summary> Read buffer </summary>
    private readonly char[] buffer = new char[128];
    /// <summary> Current read index </summary>
    private int index;
    /// <summary> Current buffer length </summary>
    private int length;

    /// <inheritdoc />
    public async ValueTask<char> Read(CancellationToken token = default)
    {
        // Fetch input if needed
        await FetchInput(token);

        // Filter out carriage return
        while (this.buffer[this.index] is '\r')
        {
            this.index++;
            await FetchInput(token);
        }

        // Return character
        return this.buffer[this.index++];
    }

    /// <summary>
    /// Fetch <see cref="Console"/> input as needed
    /// </summary>
    /// <param name="token">Cancellation token</param>
    private async ValueTask FetchInput(CancellationToken token)
    {
        while (this.index >= this.length)
        {
            this.index = 0;
            this.length = await Console.In.ReadAsync(this.buffer, token);
        }
    }

    /// <inheritdoc />
    public Task Write(char value, CancellationToken token = default) => Console.Out.WriteAsync(value);
}
