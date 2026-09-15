using Microsoft.Extensions.Logging;

namespace Synacor;

public partial class VirtualMachine
{
    [LoggerMessage(LogLevel.Trace, "File load length: {Size} bytes")]
    static partial void LogLoadFileSize(ILogger logger, int size);

    [LoggerMessage(LogLevel.Critical, "An unimplemented Opcode ({Opcode} {OpcodeValue}) has been reached, the Virtual Machine will terminate...")]
    static partial void LogUnimplementedOpcode(ILogger logger, string opcode, int opcodeValue);

    [LoggerMessage(LogLevel.Error, "An unknown Opcode value ({Opcode}) has been found, aborting...")]
    static partial void LogUnknownOpcode(ILogger logger, int opcode);

    [LoggerMessage(LogLevel.Warning, "The VirtualMachine's operation has been cancelled")]
    static partial void LogOperationCancelled(ILogger logger);

    [LoggerMessage(LogLevel.Critical, "Tried to pop a value from the stack but it was empty")]
    static partial void LogStackEmpty(ILogger logger);
}
