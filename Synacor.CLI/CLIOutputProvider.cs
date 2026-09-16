using System.Text;
using DotMake.CommandLine;

namespace Synacor.CLI;

/// <summary>
/// <see cref="CliWriter"/> output provider
/// </summary>
/// <param name="writer">Writer instance</param>
public sealed class CliOutputProvider(CliWriter writer) : IOutputProvider, IDisposable, IAsyncDisposable
{
    /// <summary> Writer instance </summary>
    private readonly CliWriter writer = writer;
    /// <summary> <see cref="StringBuilder"/> write buffer </summary>
    private readonly StringBuilder builder = new(512);

    /// <inheritdoc />
    public async ValueTask Write(char value, CancellationToken token = default)
    {
        // Append character
        this.builder.Append(value);

        // Flush the writer when a newline is encountered
        if (value is '\n')
        {
            await this.writer.WriteAsync(this.builder, token);
            this.builder.Clear();
        }
    }

    /// <inheritdoc />
    public async ValueTask Flush(CancellationToken token = default)
    {
        if (this.builder.Length > 0)
        {
            await this.writer.WriteAsync(this.builder, token);
            this.builder.Clear();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (this.builder.Length > 0)
        {
            this.writer.Write(this.builder);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() => await Flush();
}
