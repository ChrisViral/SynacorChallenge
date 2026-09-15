using JetBrains.Annotations;

namespace Synacor;

/// <summary>
/// <see cref="VirtualMachine"/> input provider
/// </summary>
[PublicAPI]
public interface IInputProvider
{
    /// <summary>
    /// Reads a character from the input
    /// </summary>
    /// <param name="token">The cancellation token</param>
    /// <returns>Character read from the input</returns>
    ValueTask<char> Read(CancellationToken token = default);
}
