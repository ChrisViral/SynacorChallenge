using Microsoft.Extensions.Logging;

namespace Synacor;

/// <summary>
/// Virtual Machine implementation
/// </summary>
public class VirtualMachine
{
    private readonly FileInfo data;

    /// <summary>
    /// Logger instance
    /// </summary>
    private ILogger Logger { get; }

    /// <summary>
    /// Creates a new Virtual Machine
    /// </summary>
    /// <param name="data">VM Data file</param>
    /// <param name="logger">Logger instance</param>
    /// <exception cref="ArgumentException">If <paramref name="data"/> does not exist</exception>
    public VirtualMachine(FileInfo data, ILogger<VirtualMachine> logger)
    {
        if (!data.Exists) throw new ArgumentException("Data file does not exist", nameof(data));

        this.data = data;
        this.Logger = logger;
    }

    /// <summary>
    /// Hello, World!
    /// </summary>
    public void SayHello() => this.Logger.LogInformation("Hello, World!\nData file: {Path}", this.data.FullName);
}
