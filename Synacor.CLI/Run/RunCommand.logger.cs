using Microsoft.Extensions.Logging;

namespace Synacor.CLI.Run;

public partial class RunCommand
{
    [LoggerMessage(LogLevel.Information, "Creating Virtual Machine and loading file data...")]
    static partial void LogCreateVM(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Running Virtual Machine, press Ctrl+C at any time to exit...")]
    static partial void LogRunVM(ILogger logger);

    [LoggerMessage(LogLevel.Error, "Exception occured while runing the Virtual Machine, exiting...")]
    static partial void LogVMThrewException(ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Information, "Dumping Virtual Machine state to {File}")]
    static partial void LogDumpingState(ILogger logger, string file);

    [LoggerMessage(LogLevel.Information, "Virtual Machine final register set to {Value}")]
    static partial void LogRegisterSet(ILogger logger, ushort value);

    [LoggerMessage(LogLevel.Information, "Patching register {Register} to {Value}")]
    static partial void LogApplyRegisterPatch(ILogger logger, char register, ushort value);

    [LoggerMessage(LogLevel.Information, "Patching memory address {Address} to Opcode {Opcode}")]
    static partial void LogApplyMemoryPatch(ILogger logger, int address, Opcode opcode);

    [LoggerMessage(LogLevel.Information, "Loading Virtual Machine memory state from {File}")]
    static partial void LoadLoadState(ILogger logger, string file);

    [LoggerMessage(LogLevel.Information, "Loading Virtual Machine program from {File}")]
    static partial void LogLoadMemory(ILogger logger, string file);
}
