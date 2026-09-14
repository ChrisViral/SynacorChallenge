using System.Runtime.InteropServices;
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
    /// Max numerical value
    /// </summary>
    private const int MAX_VALUE = short.MaxValue;
    /// <summary>
    /// Memory size, in values
    /// </summary>
    private const int MEMORY_SIZE = MAX_VALUE + 1;
    /// <summary>
    /// Amount of registers
    /// </summary>
    private const int REGISTER_COUNT = 8;
    /// <summary>
    /// Total buffer size, in values
    /// </summary>
    private const int BUFFER_SIZE = MEMORY_SIZE + REGISTER_COUNT;

    private Stack stack = new();
    private MemoryManager memoryManager;
    private unsafe Value* memory;
    private unsafe Value* ip;
    private unsafe Value* registers;
    private bool hasData;

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
    public unsafe VirtualMachine(ILogger<VirtualMachine> logger)
    {
        this.Logger = logger;

        this.memoryManager = new MemoryManager(BUFFER_SIZE);
        this.memory = this.memoryManager.Buffer;
        this.ip = this.memory;
        this.registers = this.memory + MEMORY_SIZE;
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
        this.Logger.LogInformation("Loading data file into Virtual Machine memory...");
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
    /// Resets this <see cref="VirtualMachine"/> to it's default state
    /// </summary>
    /// <exception cref="ObjectDisposedException">If this <see cref="VirtualMachine"/> has been disposed</exception>
    public unsafe void Reset()
    {
        ObjectDisposedException.ThrowIf(this.IsDisposed, this);

        // Reset instruction pointer and stack
        this.stack.Clear();
        this.ip = this.memory;
        if (!this.hasData) return;

        // Clear memory
        this.memoryManager.Clear();
        this.hasData = false;
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
