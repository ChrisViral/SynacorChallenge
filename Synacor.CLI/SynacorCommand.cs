using DotMake.CommandLine;
using Microsoft.Extensions.Logging;

namespace Synacor.CLI;

/// <summary>
/// Synacor Challenge Virtual Machine interface
/// </summary>
[CliCommand(Description = "Synacor Challenge Virtual Machine interface")]
public partial class SynacorCommand(ILoggerFactory factory, ConsoleProvider provider) : ICliRunAsyncWithContextAndReturn
{
    private readonly ILoggerFactory factory = factory;
    private readonly ConsoleProvider provider = provider;

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
        try
        {
            LogCreateVM(this.Logger);
            using VirtualMachine vm = new(this.factory.CreateLogger<VirtualMachine>(), this.provider, this.provider);
            await vm.LoadFile(this.Data, cliContext.CancellationToken);

            LogRunVM(this.Logger);
            int result = await vm.Run(cliContext.CancellationToken);
            return result;
        }
        catch (Exception e)
        {
            LogVMThrewException(this.Logger, e);
            return 1;
        }
    }
}
