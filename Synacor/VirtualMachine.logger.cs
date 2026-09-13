using Microsoft.Extensions.Logging;

namespace Synacor;

public partial class VirtualMachine
{
    [LoggerMessage(LogLevel.Information, "Data length: {Size} bytes")]
    static partial void LogLoadFileSize(ILogger logger, int size);
}