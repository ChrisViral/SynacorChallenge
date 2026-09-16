namespace Synacor;

/// <summary>
/// <see cref="VirtualMachine"/> state
/// </summary>
public enum State : byte
{
    /// <summary>
    /// The <see cref="VirtualMachine"/> has no data loaded
    /// </summary>
    EMPTY,
    /// <summary>
    /// The <see cref="VirtualMachine"/> is in an idle, unstarted state, but that can be started
    /// </summary>
    IDLE,
    /// <summary>
    /// The <see cref="VirtualMachine"/> is currently running
    /// </summary>
    RUNNING,
    /// <summary>
    /// The <see cref="VirtualMachine"/> is waiting for IO
    /// </summary>
    IO,
    /// <summary>
    /// The <see cref="VirtualMachine"/> is halted
    /// </summary>
    HALTED,
    /// <summary>
    /// If the operations of this <see cref="VirtualMachine"/> has been forcefully cancelled
    /// </summary>
    CANCELLED,
    /// <summary>
    /// An error occured that caused the <see cref="VirtualMachine"/> to fail
    /// </summary>
    ERROR
}
