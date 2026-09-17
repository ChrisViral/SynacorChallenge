using Microsoft.Extensions.Logging;

namespace Synacor.CLI.Orb;

public partial class OrbCommand
{
    [LoggerMessage(LogLevel.Error, "Graph file {File} does not exist...")]
    static partial void LogFileDoesNotExist(ILogger logger, string file);

    [LoggerMessage(LogLevel.Information, "Parsing graph file {File} into grid...")]
    static partial void LogParsingGraph(ILogger logger, string file);

    [LoggerMessage(LogLevel.Error, "Graph file could not be parsed into proper grid")]
    static partial void LogParsingGraphFailed(ILogger logger);

    [LoggerMessage(LogLevel.Error, "Could not find a valid path for the orb")]
    static partial void LogNoPathFound(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Found valid orb path: {Operation} = {Result}")]
    static partial void LogPathFound(ILogger logger, string operation, int result);
}