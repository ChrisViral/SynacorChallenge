using JetBrains.Annotations;

namespace Synacor;

/// <summary>
/// <see cref="VirtualMachine"/> output provider
/// </summary>
[PublicAPI]
public interface IOutputProvider
{
    /// <summary>
    /// Writes the given character to the output
    /// </summary>
    /// <param name="value">Value to write</param>
    void Write(char value);
}
