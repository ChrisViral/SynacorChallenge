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
    /// <param name="token">The canbcellation token</param>
    /// <returns>Character read from the input</returns>
    Task<char> Read(CancellationToken token = default);
}
