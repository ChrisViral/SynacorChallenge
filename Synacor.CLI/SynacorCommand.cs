using DotMake.CommandLine;

namespace Synacor.CLI;

/// <summary>
/// Synacor Challenge Virtual Machine interface
/// </summary>
[CliCommand(Description = "Synacor Challenge Virtual Machine interface")]
public sealed class SynacorCommand : ICliRunWithContext
{
    /// <inheritdoc />
    public void Run(CliContext context)
    {
        if (!context.Result.HasArgs)
        {
            context.ShowHelp();
        }

#if DEBUG
        context.ShowHierarchy();
#endif
    }
}
