using DotMake.CommandLine;
using Microsoft.Extensions.Logging;

namespace Synacor.CLI;

/// <summary>
/// Synacor Challenge Virtual Machine interface
/// </summary>
[CliCommand(Description = "Synacor Challenge Virtual Machine interface")]
public partial class SynacorCommand(ILoggerFactory factory, ConsoleInputProvider inputProvider) : ICliRunAsyncWithContextAndReturn
{
    private readonly ILoggerFactory factory = factory;
    private readonly ConsoleInputProvider inputProvider = inputProvider;

    /// <summary>
    /// Logger instance
    /// </summary>
    private ILogger Logger { get; } = factory.CreateLogger<SynacorCommand>();

    /// <summary>
    /// Virtual Machine binaries file
    /// </summary>
    [CliArgument(Description = "Virtual Machine binaries file", Arity = CliArgumentArity.ExactlyOne,
                 ValidationRules = CliValidationRules.ExistingFile)]
    public required FileInfo Data { get; set; }

    /// <summary>
    /// If the file being loaded is a Virtual Machine state and not simple binaries
    /// </summary>
    [CliOption(Description = "If the file being loaded is a Virtual Machine state and not simple binaries", Arity = CliArgumentArity.ZeroOrOne)]
    public bool FromState { get; set; }

    /// <inheritdoc />
    public async Task<int> RunAsync(CliContext cliContext)
    {
        CliOutputProvider outputProvider = new(cliContext.Output);
        try
        {
            LogCreateVM(this.Logger);
            using VirtualMachine vm = new(this.factory.CreateLogger<VirtualMachine>(), this.inputProvider, outputProvider);

            // Dump data when aborted
            Console.CancelKeyPress += (_, _) =>
            {
                // ReSharper disable AccessToDisposedClosure
                vm.Abort();
                vm.DumpStateToFile(new FileInfo(this.Data + ".vmd")).GetAwaiter().GetResult();
                // ReSharper restore AccessToDisposedClosure
            };

            // Load data
            if (this.FromState)
            {
                await vm.LoadStateFromFile(this.Data, cliContext.CancellationToken);
            }
            else
            {
                await vm.LoadFile(this.Data, cliContext.CancellationToken);
            }

            LogRunVM(this.Logger);
            int result = await vm.Run(cliContext.CancellationToken);
            return result;
        }
        catch (OperationCanceledException)
        {
            // Cancellation should not produce an error
            LogVirtualMachineOperationCancelled(this.Logger);
            return 0;
        }
        catch (Exception e)
        {
            await outputProvider.Flush(cliContext.CancellationToken);
            LogVMThrewException(this.Logger, e);
            return 1;
        }
    }
}
