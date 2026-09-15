using Microsoft.Extensions.Logging;

namespace Synacor.CLI;

public partial class SynacorCommand
{
    [LoggerMessage(LogLevel.Information, "Creating virtual machine...")]
    static partial void LogCreateVM(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Running Virtual Machine...")]
    static partial void LogRunVM(ILogger logger);

    [LoggerMessage(LogLevel.Error, "Exception occured while runing the Virtual Machine, exiting...")]
    static partial void LogVMThrewException(ILogger logger, Exception exception);
}