using DotMake.CommandLine;
using FastEnumUtility;
using Microsoft.Extensions.Logging;
using Synacor.Data;

namespace Synacor.CLI;

/// <summary>
/// <see cref="VirtualMachine"/> memory patch
/// </summary>
public readonly record struct Patch
{
    /// <summary>
    /// Patch index
    /// </summary>
    public ushort Index { get; init; }

    /// <summary>
    /// Patch <see cref="Opcode"/>
    /// </summary>
    public Opcode Opcode { get; init; }

    /// <summary>
    /// Creates a new patch from the given data
    /// </summary>
    /// <param name="data">Data to create the patch from</param>
    public Patch(string data)
    {
        ReadOnlySpan<char> dataSpan = data;
        int separatorIndex = dataSpan.IndexOf('-');
        this.Index = ushort.Parse(dataSpan[..separatorIndex]);
        this.Opcode = FastEnum.Parse<Opcode>(dataSpan[(separatorIndex + 1)..]);
    }
}

/// <summary>
/// Synacor Challenge Virtual Machine interface
/// </summary>
[CliCommand(Name = "run", Description = "Virtual Machine run command", Parent = typeof(SynacorCommand))]
public sealed partial class RunCommand(ILoggerFactory factory, ConsoleInputProvider inputProvider) : ICliRunAsyncWithContextAndReturn
{
    private readonly ILoggerFactory factory = factory;
    private readonly ConsoleInputProvider inputProvider = inputProvider;

    /// <summary>
    /// Logger instance
    /// </summary>
    private ILogger Logger { get; } = factory.CreateLogger<RunCommand>();

    /// <summary>
    /// Virtual Machine binaries file
    /// </summary>
    [CliArgument(Description = "Virtual Machine binaries file", ValidationRules = CliValidationRules.ExistingFile)]
    public required FileInfo Data { get; set; }

    /// <summary>
    /// If the file being loaded is a Virtual Machine state and not simple binaries
    /// </summary>
    [CliOption(Description = "If the file being loaded is a Virtual Machine state and not simple binaries")]
    public bool FromState { get; set; }

    /// <summary>
    /// If the Virtual Machine state should be saved on cancel
    /// </summary>
    [CliOption(Description = "If the Virtual Machine state should be saved on cancel")]
    public bool SaveState { get; set; }

    /// <summary>
    /// Value to initialize the eight register to
    /// </summary>
    [CliOption(Description = "Value to initialize the eight register to", Arity = CliArgumentArity.ZeroOrOne)]
    public ushort? TeleporterValue { get; set; }

    /// <summary>
    /// Virtual Machine memory patches
    /// </summary>
    [CliOption(Description = "Virtual Machine memory patches (formatted Index-Opcode)", AllowMultipleArgumentsPerToken = true, ValidationPattern = @"\d{1,5}-[A-Za-z]{2,4}")]
    public Patch[] Patches { get; set; } = [];

    /// <inheritdoc />
    public async Task<int> RunAsync(CliContext cliContext)
    {
        CliOutputProvider outputProvider = new(cliContext.Output);
        VirtualMachine? vm = null;
        try
        {
            LogCreateVM(this.Logger);
            vm = new VirtualMachine(this.factory.CreateLogger<VirtualMachine>(), this.inputProvider, outputProvider);

            // Dump data when aborted
            Console.CancelKeyPress += (_, e) =>
            {
                // ReSharper disable once AccessToDisposedClosure
                vm.Abort();
                e.Cancel = true;
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

            // Set last register requested
            if (this.TeleporterValue is { } value and <= Value.MAX_VALUE)
            {
                LogRegisterSet(this.Logger, value);
                vm.Registers[^1] = value;
            }

            // Patch memory
            foreach (Patch patch in this.Patches)
            {
                vm.Memory[patch.Index] = patch.Opcode;
            }

            // Start VM
            LogRunVM(this.Logger);
            int result = await vm.Run(cliContext.CancellationToken);
            return result;
        }
        catch (OperationCanceledException)
        {
            // Dump state on cancellation
            if (vm is not null && this.SaveState)
            {
                LogDumpingState(this.Logger);
                await vm.DumpStateToFile(new FileInfo(this.Data + ".vmd"));
            }

            // Cancellation should not produce an error
            return 0;
        }
        catch (Exception e)
        {
            await outputProvider.Flush(cliContext.CancellationToken);
            LogVMThrewException(this.Logger, e);
            return 1;
        }
        finally
        {
            vm?.Dispose();
        }
    }
}
