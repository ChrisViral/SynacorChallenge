using Microsoft.Extensions.Logging;

namespace Synacor;

public partial class VirtualMachine
{
    [LoggerMessage(LogLevel.Trace, "File load length: {Size} bytes")]
    static partial void LogLoadFileSize(ILogger logger, int size);

    [LoggerMessage(LogLevel.Critical, "An unknown Opcode value ({Opcode}) has been found, aborting...")]
    static partial void LogUnknownOpcode(ILogger logger, int opcode);

    [LoggerMessage(LogLevel.Warning, "The VirtualMachine's operation has been cancelled, press enter to terminate...")]
    static partial void LogOperationCancelled(ILogger logger);

    [LoggerMessage(LogLevel.Critical, "Tried to pop a value from the stack but it was empty")]
    static partial void LogStackEmpty(ILogger logger);

    [LoggerMessage(LogLevel.Error, "Virtual machine terminated without halting")]
    static partial void LogUnexpectedTermination(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Virtual Machine halted")]
    static partial void LogHalted(ILogger logger);

    [LoggerMessage(LogLevel.Warning, "Virtual Machine not loaded with data, nothing to save...")]
    static partial void LogNoDatatoSave(ILogger logger);

    [LoggerMessage(LogLevel.Warning, "Cannot save halted or errored Virtual Machine state")]
    static partial void LogCannotSaveHaltedState(ILogger logger);
}
