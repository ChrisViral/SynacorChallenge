using System.ComponentModel;
using System.Runtime.InteropServices;
using FastEnumUtility;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Synacor.Data;

namespace Synacor;

/// <summary>
/// Virtual Machine implementation
/// </summary>
[PublicAPI]
public sealed partial class VirtualMachine : IDisposable
{
    /// <summary>
    /// Memory size, in values
    /// </summary>
    private const int MEMORY_SIZE = 1 << Value.BIT_COUNT;
    /// <summary>
    /// Total buffer size, in values
    /// </summary>
    private const int BUFFER_SIZE = MEMORY_SIZE + Value.REGISTER_COUNT;

    private Stack stack = new();
    private MemoryManager memoryManager;
    private unsafe Value* memory;
    private unsafe Value* ip;
    private unsafe Value* registers;
    private bool hasData;

    private readonly IInputProvider input;
    private readonly IOutputProvider output;

    /// <summary>
    /// The current state of this <see cref="VirtualMachine"/>
    /// </summary>
    public State State { get; private set; }

    /// <summary>
    /// If this <see cref="VirtualMachine"/> has been disposed
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Logger instance
    /// </summary>
    private ILogger Logger { get; }

    /// <summary>
    /// Creates a new <see cref="VirtualMachine"/>
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="input">Input provider</param>
    /// <param name="output">Output provider</param>
    public unsafe VirtualMachine(ILogger<VirtualMachine> logger, IInputProvider input, IOutputProvider output)
    {
        this.Logger = logger;

        this.memoryManager = new MemoryManager(BUFFER_SIZE);
        this.memory = this.memoryManager.Buffer;
        this.ip = this.memory;
        this.registers = this.memory + MEMORY_SIZE;

        this.input = input;
        this.output = output;
    }

    /// <summary>
    /// Deallocates unmanaged memory before being collected
    /// </summary>
    ~VirtualMachine() => ReleaseUnmanagedResources();

    /// <summary>
    /// Loads the binary data file into the <see cref="VirtualMachine"/>'s memory
    /// </summary>
    /// <param name="file">File to load</param>
    /// <param name="token">Cancellation token</param>
    /// <exception cref="ObjectDisposedException">If this <see cref="VirtualMachine"/> has been disposed</exception>
    /// <exception cref="FileNotFoundException">If the <paramref name="file"/> to load doesn't exist</exception>
    /// <exception cref="ArgumentException">If the <paramref name="file"/> is too large to fit into the <see cref="VirtualMachine"/>'s memory</exception>
    public async Task LoadFile(FileInfo file, CancellationToken token = default)
    {
        ObjectDisposedException.ThrowIf(this.IsDisposed, this);
        if (!file.Exists) throw new FileNotFoundException("Data file to load does not exist", file.FullName);
        if (file.Length > MEMORY_SIZE * Value.SIZE) throw new ArgumentException("File size too large for Virtual Machine memory", nameof(file));

        // Get memory view
        int length = (int)file.Length;
        using MemoryView<byte> view = new(this.memoryManager, 0, length);

        // Clear the data if there is any
        if (this.hasData)
        {
            Reset();
        }

        // Load data
        LogLoadFileSize(this.Logger, length);
        await using FileStream stream = file.OpenRead();
        await stream.ReadExactlyAsync(view.Memory, token).ConfigureAwait(false);
        this.hasData = true;
    }

    /// <summary>
    /// Loads data into the V<see cref="VirtualMachine"/>'s memory from a given data span
    /// </summary>
    /// <param name="data">Data to load</param>
    /// <exception cref="ObjectDisposedException">If this <see cref="VirtualMachine"/> has been disposed</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="data"/> is too large to fit into the <see cref="VirtualMachine"/>'s memory</exception>
    public void LoadData(ReadOnlySpan<Value> data)
    {
        ObjectDisposedException.ThrowIf(this.IsDisposed, this);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(data.Length, MEMORY_SIZE, nameof(data));

        // Clear the data if there is any
        if (this.hasData)
        {
            Reset();
        }

        // Copy data to memory
        data.CopyTo(this.memoryManager.GetSpan());
        this.hasData = true;
    }

    /// <summary>
    /// Loads data into the <see cref="VirtualMachine"/>'s memory from a given span
    /// </summary>
    /// <param name="data">Data to load</param>
    /// <typeparam name="T">Incoming data type</typeparam>
    /// <exception cref="ObjectDisposedException">If this <see cref="VirtualMachine"/> has been disposed</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="data"/> is too large to fit into the <see cref="VirtualMachine"/>'s memory</exception>
    public void LoadData<T>(ReadOnlySpan<T> data) where T : unmanaged
    {
        ObjectDisposedException.ThrowIf(this.IsDisposed, this);

        // Cast incoming data to Value
        ReadOnlySpan<Value> castedData = MemoryMarshal.Cast<T, Value>(data);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(castedData.Length, MEMORY_SIZE, nameof(data));

        // Clear the data if there is any
        if (this.hasData)
        {
            Reset();
        }

        // Copy data to memory
        castedData.CopyTo(this.memoryManager.GetSpan());
        this.hasData = true;
    }

    /// <summary>
    /// Starts the <see cref="VirtualMachine"/>'s program
    /// </summary>
    /// <param name="token">Cancellation token</param>
    /// <exception cref="OperationCanceledException">If the operation is cancelled via</exception>
    /// ReSharper disable once CognitiveComplexity
    public unsafe void Run(CancellationToken token)
    {
        this.State = State.RUNNING;
        while (this.State is State.RUNNING)
        {
            if (token.IsCancellationRequested)
            {
                LogOperationCancelled(this.Logger);
                this.State = State.CANCELLED;
                token.ThrowIfCancellationRequested();
            }

            Opcode opcode = *this.ip++;
            switch (opcode)
            {
                case Opcode.HALT: // 0
                    this.State = State.HALTED;
                    break;

                case Opcode.OUT: // 19
                    this.output.Write((char)*this.ip++);
                    break;

                case Opcode.NOOP: // 21
                    break;

                default:
                    this.State = State.ERROR;
                    if (FastEnum.IsDefined(opcode))
                    {
                        LogUnimplementedOpcode(this.Logger, opcode.FastToString(), (int)opcode);
                        throw new NotImplementedException($"Opcode {opcode.FastToString()} not yet implemented");
                    }

                    LogUnknownOpcode(this.Logger, (int)opcode);
                    throw new InvalidEnumArgumentException(nameof(opcode), (int)opcode, typeof(Opcode));
            }
        }
    }

    /// <summary>
    /// Resets this <see cref="VirtualMachine"/> to it's default state
    /// </summary>
    /// <exception cref="ObjectDisposedException">If this <see cref="VirtualMachine"/> has been disposed</exception>
    public unsafe void Reset()
    {
        ObjectDisposedException.ThrowIf(this.IsDisposed, this);

        // Reset instruction, stack, and state
        this.stack.Clear();
        this.ip = this.memory;
        this.State = State.IDLE;

        // Clear memory
        if (this.hasData)
        {
            this.memoryManager.Clear();
            this.hasData = false;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (this.IsDisposed) return;

        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
        this.IsDisposed = true;
    }

    /// <summary>
    /// Deallocates unmanaged memory
    /// </summary>
    private unsafe void ReleaseUnmanagedResources()
    {
        this.stack.Dispose();
        ((IDisposable)this.memoryManager).Dispose();

        this.stack = null!;
        this.memoryManager = null!;
        this.memory = null;
        this.ip = null;
        this.registers = null;
    }
}
