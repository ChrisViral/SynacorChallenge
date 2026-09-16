using Microsoft.Extensions.Logging;

namespace Synacor.CLI;

public partial class TeleportCommand
{
    [LoggerMessage(LogLevel.Debug, "Checking teleporters in range {Start} - {End}")]
    static partial void LogRegisterRange(ILogger logger, int start, int end);

    [LoggerMessage(LogLevel.Information, "Finding valid eighth register value for teleport with parameters A = {A}, B = {B}, and Result = {Result}...")]
    static partial void LogFindingRegister(ILogger logger, int a, int b, int result);

    [LoggerMessage(LogLevel.Information, "Found matching register value: {Register}")]
    static partial void LogFoundRegister(ILogger logger, int register);

    [LoggerMessage(LogLevel.Error, "Could not find matching register value...")]
    static partial void LogDidNotFindRegister(ILogger logger);
}
