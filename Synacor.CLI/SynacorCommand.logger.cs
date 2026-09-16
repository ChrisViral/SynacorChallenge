using Microsoft.Extensions.Logging;

namespace Synacor.CLI;

public partial class SynacorCommand
{
    [LoggerMessage(LogLevel.Information, "Creating Virtual Machine and loading file data...")]
    static partial void LogCreateVM(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Running Virtual Machine, press Ctrl+C at any time to exit...")]
    static partial void LogRunVM(ILogger logger);

    [LoggerMessage(LogLevel.Error, "Exception occured while runing the Virtual Machine, exiting...")]
    static partial void LogVMThrewException(ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Information, "Dumping Virtual Machine state...")]
    static partial void LogDumpingState(ILogger logger);
}
