using DotMake.CommandLine;
using Microsoft.Extensions.Logging;

namespace Synacor.CLI;

/// <summary>
/// Synacor Challenge Virtual Machine interface
/// </summary>
[CliCommand(Description = "Synacor Challenge Virtual Machine interface")]
public class SynacorCommand(ILoggerFactory factory) : ICliRunAsyncWithContextAndReturn
{
    private readonly ILoggerFactory factory = factory;

    /// <summary>
    /// Logger instance
    /// </summary>
    private ILogger Logger { get; } = factory.CreateLogger<SynacorCommand>();

    /// <summary>
    /// VM data file
    /// </summary>
    [CliArgument(Description = "VM data file", Arity = CliArgumentArity.ExactlyOne,
                 ValidationRules = CliValidationRules.ExistingFile, ValidationPattern = @".+\.bin")]
    public required FileInfo Data { get; set; }

    /// <inheritdoc />
    public async Task<int> RunAsync(CliContext cliContext)
    {
        VirtualMachine vm = new(this.Data, this.factory.CreateLogger<VirtualMachine>());
        vm.SayHello();
        return 0;
    }
}
