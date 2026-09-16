using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Synacor.Data;
using Synacor.Memory;

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

    /// <summary> Memory stack </summary>
    private Stack stack = new();
    /// <summary> Memory block manager </summary>
    private MemoryManager memoryManager;
    /// <summary> Memory buffer start pointer </summary>
    private unsafe Value* memory;
    /// <summary> Instruction pointer </summary>
    private unsafe Value* ip;
    /// <summary> Registers first pointer </summary>
    private unsafe Value* registers;
    /// <summary> Last opcode read pointer </summary>
    private unsafe Value* lastOpcode;

    /// <summary> Input provider </summary>
    private readonly IInputProvider input;
    /// <summary> Output provider </summary>
    private readonly IOutputProvider output;
    /// <summary> Run cancellation token source </summary>
    private CancellationTokenSource? runCancellationSource;

    /// <summary>
    /// The current state of this <see cref="VirtualMachine"/>
    /// </summary>
    public State State { get; private set; }

    /// <summary>
    /// If this <see cref="VirtualMachine"/> has been disposed
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// If this <see cref="VirtualMachine"/> is currentl active and running code
    /// </summary>
    public bool IsActive => this.State is State.RUNNING or State.IO;

    /// <summary>
    /// Program memory block
    /// </summary>
    public unsafe Span<Value> Memory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(this.memory, Value.MAX_VALUE + 1);
    }

    /// <summary>
    /// Registers memory block
    /// </summary>
    public unsafe Span<Value> Registers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(this.registers, Value.REGISTER_COUNT);
    }

    /// <summary>
    /// Current memory address
    /// </summary>
    private unsafe ushort Address
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (ushort)(this.ip - this.memory);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            this.ip = this.memory + value;
            this.lastOpcode = this.ip;
        }
    }

    /// <summary>
    /// Last <see cref="Opcode"/> memory address
    /// </summary>
    private unsafe ushort LastOpcodeAddress
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (ushort)(this.lastOpcode - this.memory);
    }

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
        this.lastOpcode = this.ip;

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

        // Make sure we can load data
        if (this.IsActive)
        {
            LogCannotLoadWhileRunning(this.Logger);
            return;
        }

        // Get memory view
        int length = (int)file.Length;
        using MemoryView<byte> view = new(this.memoryManager, 0, length);

        // Clear the data if there is any
        if (this.State is not State.EMPTY)
        {
            Reset();
        }

        // Load data
        LogLoadFileSize(this.Logger, length);
        await using FileStream stream = file.OpenRead();
        await stream.ReadExactlyAsync(view.Memory, token).ConfigureAwait(false);
        this.State = State.IDLE;
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

        // Make sure we can load data
        if (this.IsActive)
        {
            LogCannotLoadWhileRunning(this.Logger);
            return;
        }

        // Clear the data if there is any
        if (this.State is not State.EMPTY)
        {
            Reset();
        }

        // Copy data to memory
        data.CopyTo(this.memoryManager.GetSpan());
        this.State = State.IDLE;
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

        // Make sure we can load data
        if (this.IsActive)
        {
            LogCannotLoadWhileRunning(this.Logger);
            return;
        }

        // Cast incoming data to Value
        ReadOnlySpan<Value> castedData = MemoryMarshal.Cast<T, Value>(data);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(castedData.Length, MEMORY_SIZE, nameof(data));

        // Clear the data if there is any
        if (this.State is not State.EMPTY)
        {
            Reset();
        }

        // Copy data to memory
        castedData.CopyTo(this.memoryManager.GetSpan());
        this.State = State.IDLE;
    }

    /// <summary>
    /// Loads a <see cref="VirtualMachine"/> state from a file
    /// </summary>
    /// <param name="file">File to load the state from</param>
    /// <param name="token">Cancellation token</param>
    /// <exception cref="ObjectDisposedException">If this <see cref="VirtualMachine"/> has been disposed</exception>
    /// <exception cref="FileNotFoundException">If the <paramref name="file"/> to load doesn't exist</exception>
    public async Task LoadStateFromFile(FileInfo file, CancellationToken token = default)
    {
        ObjectDisposedException.ThrowIf(this.IsDisposed, this);
        if (!file.Exists) throw new FileNotFoundException("Data file to load does not exist", file.FullName);

        // Make sure we can load data
        if (this.IsActive)
        {
            LogCannotLoadWhileRunning(this.Logger);
            return;
        }

        // Reset data before loading if not empty
        if (this.State is not State.EMPTY)
        {
            Reset();
        }

        // Setup file reading
        await using FileStream fileStream = file.OpenRead();
        using BinaryReader reader = new(fileStream);

        // Read address info
        this.Address = reader.ReadUInt16();

        // Read stack info
        int stackCount = reader.ReadInt32();
        int stackCapacity = reader.ReadInt32();

        // Setup stack size
        this.stack.EnsureCapacity(stackCapacity);
        this.stack.ForceSetCount(stackCount);

        // Read stack contents
        using StackView stackView = new(this.stack);
        await fileStream.ReadExactlyAsync(stackView.Memory, token).ConfigureAwait(false);

        // Read memory contents
        using MemoryView<byte> memoryView = new(this.memoryManager, 0, (int)this.memoryManager.ByteLength);
        await fileStream.ReadExactlyAsync(memoryView.Memory, token).ConfigureAwait(false);

        // Set state
        this.State = State.IDLE;
    }

    /// <summary>
    /// Dump the current <see cref="VirtualMachine"/> state to a file
    /// </summary>
    /// <param name="file">File to dump the state to</param>
    /// <param name="token">Cancellation token</param>
    /// <exception cref="ObjectDisposedException">If this <see cref="VirtualMachine"/> has been disposed</exception>
    public async Task DumpStateToFile(FileInfo file, CancellationToken token = default)
    {
        ObjectDisposedException.ThrowIf(this.IsDisposed, this);

        // Make sure there's something to save
        if (this.State is State.EMPTY)
        {
            LogNoDatatoSave(this.Logger);
            return;
        }

        // Make sure we're not in an errored state
        if (this.State is State.HALTED or State.ERROR)
        {
            LogCannotSaveHaltedState(this.Logger);
            return;
        }

        // Setup file writing
        await using FileStream fileStream = file.OpenWrite();
        await using BinaryWriter writer = new(fileStream);

        // Write address info
        writer.Write(this.LastOpcodeAddress);

        // Write stack info
        writer.Write(this.stack.Count);
        writer.Write(this.stack.Capacity);

        // Write stack contents (from stack bottom to stack top)
        using StackView stackView = new(this.stack);
        await fileStream.WriteAsync(stackView.Memory, token).ConfigureAwait(false);

        // Write entire memory contents
        using MemoryView<byte> memoryView = new(this.memoryManager, 0, (int)this.memoryManager.ByteLength);
        await fileStream.WriteAsync(memoryView.Memory, token).ConfigureAwait(false);
    }

    /// <summary>
    /// Starts the <see cref="VirtualMachine"/>'s program
    /// </summary>
    /// <param name="token">Cancellation token</param>
    /// <exception cref="OperationCanceledException">If the operation is cancelled via</exception>
    /// ReSharper disable once CognitiveComplexity
    public async Task<int> Run(CancellationToken token = default)
    {
        // Make sure we are in a state where we can start the Virtual Machine
        if (this.State is not State.IDLE) return 0;

        this.runCancellationSource?.Dispose();
        CancellationTokenSource temp = CancellationTokenSource.CreateLinkedTokenSource(token);
        this.runCancellationSource = temp;
        token = this.runCancellationSource.Token;

        // Start running
        this.State = State.RUNNING;
        while (this.State is State.RUNNING)
        {
            // Check cancellation
            token.ThrowIfCancellationRequested();

            Opcode opcode = GetOpcode();
            switch (opcode)
            {
                // 0 - halt - Halt execution
                case Opcode.HALT:
                    await Halt(token).ConfigureAwait(false);
                    return 0;

                // 1 - set a b - Set register a to b
                case Opcode.SET:
                {
                    ref Value register = ref GetRegister();
                    Value a = GetValue();
                    register = a;
                    break;
                }

                // 2 - push a - Push the value a onto the stack
                case Opcode.PUSH:
                {
                    Value a = GetValue();
                    this.stack.Push(a);
                    break;
                }

                // 3 - pop a - Pop the top value of the stack and store it into a (success branch)
                case Opcode.POP when this.stack.TryPop(out Value value):
                {
                    ref Value register = ref GetRegister();
                    register = value;
                    break;
                }

                // 3 - pop a - Pop the top value of the stack and store it into a (failure branch)
                case Opcode.POP:
                    await Error(token).ConfigureAwait(false);
                    LogStackEmpty(this.Logger);
                    return 1;

                // 4 - eq a b c - Set a to 1 if b equals c, otherwise set it to 0
                case Opcode.EQ:
                {
                    ref Value register = ref GetRegister();
                    Value a = GetValue();
                    Value b = GetValue();
                    register = a == b ? Value.True : Value.False;
                    break;
                }

                // 5 - gt a b c - Set a to 1 if b is greater than c, otherwise set it to 0
                case Opcode.GT:
                {
                    ref Value register = ref GetRegister();
                    Value a = GetValue();
                    Value b = GetValue();
                    register = a > b ? Value.True : Value.False;
                    break;
                }

                // 6 - jmp a - Jump to a
                case Opcode.JMP:
                // 7 - jt a b - Jump to b if a is true (success branch)
                case Opcode.JT when GetValue() != Value.False:
                // 8 - jf a b - Jump to b if a false (success branch)
                case Opcode.JF when GetValue() == Value.False:
                    Jump();
                    break;

                // 7 - jt a b - Jump to b if a is true (failure branch)
                case Opcode.JT:
                // 8 - jf a b - Jump to b if a false (failure branch)
                case Opcode.JF:
                    MoveNext();
                    break;

                // 9 - add a b c - Store into a the sum of b and c
                case Opcode.ADD:
                {
                    ref Value register = ref GetRegister();
                    Value a = GetValue();
                    Value b = GetValue();
                    register = a + b;
                    break;
                }

                // 10 - mult a b c - Store into a the product of b and c
                case Opcode.MULT:
                {
                    ref Value register = ref GetRegister();
                    Value a = GetValue();
                    Value b = GetValue();
                    register = a * b;
                    break;
                }

                // 11 - mop a b c - Store into a the modulus of b and c
                case Opcode.MOD:
                {
                    ref Value register = ref GetRegister();
                    Value a = GetValue();
                    Value b = GetValue();
                    register = a % b;
                    break;
                }

                // 12 - and a b c - Store into a the bitwise and of b and c
                case Opcode.AND:
                {
                    ref Value register = ref GetRegister();
                    Value a = GetValue();
                    Value b = GetValue();
                    register = a & b;
                    break;
                }

                // 13 - or a b c - Store into a the bitwise or of b and c
                case Opcode.OR:
                {
                    ref Value register = ref GetRegister();
                    Value a = GetValue();
                    Value b = GetValue();
                    register = a | b;
                    break;
                }

                // 14 - not a b - Store into a the bitwise inverse of b
                case Opcode.NOT:
                {
                    ref Value register = ref GetRegister();
                    Value a = GetValue();
                    register = ~a;
                    break;
                }

                // 15 - rmem a b - Store into register a the value at memory address b
                case Opcode.RMEM:
                {
                    ref Value register = ref GetRegister();
                    Value mem = GetMemory();
                    register = mem;
                    break;
                }

                // 16 - wmem a b - Write the value of b into memory address a
                case Opcode.WMEM:
                {
                    ref Value mem = ref GetMemory();
                    Value a = GetValue();
                    mem = a;
                    break;
                }

                // 17 - call a - Write the next instruction address to the stack, then jump to a
                case Opcode.CALL:
                {
                    Value address = GetAddress(1);
                    this.stack.Push(address);
                    Jump();
                    break;
                }

                // 18 - ret - Pop the stack and jump to the address it specified, halt if the stack is empty (success branch)
                case Opcode.RET when this.stack.TryPop(out Value value):
                    Jump(value);
                    break;

                // 18 - ret - Pop the stack and jump to the address it specified, halt if the stack is empty (failure branch)
                case Opcode.RET:
                    await Halt(token).ConfigureAwait(false);
                    return 0;

                // 19 - out a - Output the value of a as an ASCII character
                case Opcode.OUT:
                {
                    Value a = GetValue();
                    this.State = State.IO;
                    await this.output.Write(a, token).ConfigureAwait(false);
                    this.State = State.RUNNING;
                    break;
                }

                // 20 - in a - Read a character from the input, and store it's ASCII value into a
                case Opcode.IN:
                {
                    this.State = State.IO;
                    await this.output.Flush(token).ConfigureAwait(false);
                    char value = await this.input.Read(token).ConfigureAwait(false);
                    this.State = State.RUNNING;

                    ref Value register = ref GetRegister();
                    register = value;
                    break;
                }

                // 21 - noop - No operation
                case Opcode.NOOP:
                    break;

                default:
                    // Unknown opcode, log error
                    await Error(token).ConfigureAwait(false);
                    LogUnknownOpcode(this.Logger, (int)opcode);
                    throw new InvalidEnumArgumentException(nameof(opcode), (int)opcode, typeof(Opcode));
            }
        }

        // Virtual Machine in an unexpected way
        await Error(token).ConfigureAwait(false);
        LogUnexpectedTermination(this.Logger);
        return 1;
    }

    /// <summary>
    /// Aborts the current Virtual Machine run if possible
    /// </summary>
    public void Abort()
    {
        // Make sure we're running before we abort
        if (!this.IsActive || this.runCancellationSource is null)
        {
            return;
        }

        this.runCancellationSource.Cancel();
        this.runCancellationSource.Dispose();
        this.runCancellationSource = null;
        this.State = State.CANCELLED;
        LogOperationCancelled(this.Logger);
    }

    /// <summary>
    /// Resets this <see cref="VirtualMachine"/> to it's default state
    /// </summary>
    /// <exception cref="ObjectDisposedException">If this <see cref="VirtualMachine"/> has been disposed</exception>
    public unsafe void Reset()
    {
        ObjectDisposedException.ThrowIf(this.IsDisposed, this);

        // Abort if currently running
        bool hasData = this.State is not State.EMPTY;
        Abort();

        // Reset instruction pointer and stack
        this.stack.Clear();
        this.ip = this.memory;
        this.lastOpcode = this.ip;

        // Clear memory if needed
        if (hasData)
        {
            this.memoryManager.Clear();
        }

        // Clear up state
        this.State = State.EMPTY;
    }

    /// <inheritdoc />
    public unsafe void Dispose()
    {
        if (this.IsDisposed) return;

        // Stop function
        Abort();

        // Release resources and clear pointers
        ReleaseUnmanagedResources();
        this.stack = null!;
        this.memoryManager = null!;
        this.memory = null;
        this.ip = null;
        this.registers = null;
        this.lastOpcode = null;

        // Finalize
        GC.SuppressFinalize(this);
        this.IsDisposed = true;
    }

    /// <summary>
    /// Deallocates unmanaged memory
    /// </summary>
    private void ReleaseUnmanagedResources()
    {
        this.stack.Dispose();
        ((IDisposable)this.memoryManager).Dispose();
    }

    [LoggerMessage(LogLevel.Warning, "Cannot load data into Virtual Machine while it is running")]
    static partial void LogCannotLoadWhileRunning(ILogger logger);
}
