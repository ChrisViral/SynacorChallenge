using JetBrains.Annotations;

namespace Synacor;

/// <summary>
/// <see cref="VirtualMachine"/> input provider
/// </summary>
[PublicAPI]
public interface IInputProvider
{
    /// <summary>
    /// Reads a line from the input
    /// </summary>
    /// <returns>Line read from the input</returns>
    string ReadLine();
}
