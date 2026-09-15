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
        VirtualMachine? vm = null;
        try
        {
            LogCreateVM(this.Logger);
            vm = new VirtualMachine(this.factory.CreateLogger<VirtualMachine>(), this.provider, this.provider);
            await vm.LoadFile(this.Data, cliContext.CancellationToken);

            LogRunVM(this.Logger);
            vm.Run(cliContext.CancellationToken);
        }
        catch (Exception e)
        {
            LogVMThrewException(this.Logger, e);
        }
        finally
        {
            vm?.Dispose();
        }

        return vm?.State is not State.ERROR ? 0 : 1;
    }
}
