using DotMake.CommandLine;
using FastEnumUtility;
using Microsoft.Extensions.Logging;

namespace Synacor.CLI;

/// <summary>
/// <see cref="VirtualMachine"/> memory patch
/// </summary>
public readonly struct RegisterPatch
{
    /// <summary>
    /// Patch index
    /// </summary>
    public int Register { get; }

    /// <summary>
    /// Patch <see cref="Opcode"/>
    /// </summary>
    public ushort Value { get; }

    /// <summary>
    /// Creates a new patch from the given data
    /// </summary>
    /// <param name="data">Data to create the patch from</param>
    public RegisterPatch(string data)
    {
        ReadOnlySpan<char> dataSpan = data;
        this.Register = dataSpan[0] - 'a';
        this.Value = ushort.Parse(dataSpan[2..]);
    }
}

/// <summary>
/// <see cref="VirtualMachine"/> memory patch
/// </summary>
public readonly struct MemoryPatch
{
    /// <summary>
    /// Patch index
    /// </summary>
    public int Address { get; }

    /// <summary>
    /// Patch <see cref="Opcode"/>
    /// </summary>
    public Opcode Opcode { get; }

    /// <summary>
    /// Creates a new patch from the given data
    /// </summary>
    /// <param name="data">Data to create the patch from</param>
    public MemoryPatch(string data)
    {
        ReadOnlySpan<char> dataSpan = data;
        int separatorIndex = dataSpan.IndexOf(':');
        this.Address = int.Parse(dataSpan[..separatorIndex]);
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
    /// Virtual Machine memory patches
    /// </summary>
    [CliOption(Description = "Virtual Machine register patches (formatted Address:Value)", AllowMultipleArgumentsPerToken = true, ValidationPattern = @"[a-h]:\d{1,5}")]
    public RegisterPatch[] RegisterPatches { get; set; } = [];

    /// <summary>
    /// Virtual Machine memory patches
    /// </summary>
    [CliOption(Description = "Virtual Machine memory patches (formatted Address:Opcode)", AllowMultipleArgumentsPerToken = true, ValidationPattern = @"\d{1,5}:[A-Za-z]{2,4}")]
    public MemoryPatch[] MemoryPatches { get; set; } = [];

    /// <inheritdoc />
    public async Task<int> RunAsync(CliContext cliContext)
    {
        await using CliOutputProvider outputProvider = new(cliContext.Output);
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
                LoadLoadState(this.Logger, this.Data.FullName);
                await vm.LoadStateFromFile(this.Data, cliContext.CancellationToken);
            }
            else
            {
                LogLoadMemory(this.Logger, this.Data.FullName);
                await vm.LoadFile(this.Data, cliContext.CancellationToken);
            }

            // Patch registers
            foreach (RegisterPatch patch in this.RegisterPatches)
            {
                LogApplyRegisterPatch(this.Logger, (char)('a' + patch.Register), patch.Value);
                vm.Registers[patch.Register] = patch.Value;
            }

            // Patch memory
            foreach (MemoryPatch patch in this.MemoryPatches)
            {
                LogApplyMemoryPatch(this.Logger, patch.Address, patch.Opcode);
                vm.Memory[patch.Address] = patch.Opcode;
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
                FileInfo dumpFile = new(this.Data + ".vmd");
                LogDumpingState(this.Logger, dumpFile.FullName);
                await vm.DumpStateToFile(dumpFile);
            }

            // Cancellation should not produce an error
            return 0;
        }
        catch (Exception e)
        {
            LogVMThrewException(this.Logger, e);
            return 1;
        }
        finally
        {
            vm?.Dispose();
        }
    }
}
