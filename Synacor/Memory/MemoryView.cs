using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Synacor.Memory.DebugViews;

namespace Synacor.Memory;

/// <summary>
/// Non-owning view over a <see cref="MemoryManager"/>
/// </summary>
/// <typeparam name="T">View value type</typeparam>
[DebuggerDisplay("Size = {length}"), DebuggerTypeProxy(typeof(MemoryManagerDebugView<>))]
internal sealed unsafe class MemoryView<T> : MemoryManager<T> where T : unmanaged
{
    private MemoryManager memoryManager;
    private T* pointer;
    private readonly int length;

    /// <summary>
    /// Creates a view at a given offset and length over the given <see cref="MemoryManager"/>
    /// </summary>
    /// <param name="memoryManager">Memory block to create the view over</param>
    /// <param name="offset">View offset, in <typeparamref name="T"/> size</param>
    /// <param name="length">View length, in <typeparamref name="T"/> size</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// If <paramref name="offset"/> is less than zero or greater than than the original block size,
    /// or if <paramref name="length"/> is les than zero or larger than the available memory size from the offset
    /// </exception>
    public MemoryView(MemoryManager memoryManager, int offset, int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, (int)memoryManager.ByteLength / sizeof(T));
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(length, ((int)memoryManager.ByteLength / sizeof(T)) - offset);

        this.memoryManager = memoryManager;
        this.pointer = (T*)memoryManager.Buffer + offset;
        this.length = length;
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">If the underlying memory has been disposed</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override Span<T> GetSpan()
    {
        ObjectDisposedException.ThrowIf(this.memoryManager.IsDisposed, this.memoryManager);

        return new Span<T>(this.pointer, this.length);
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">If the underlying memory has been disposed</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="elementIndex"/> is outside of the range of the memory block</exception>
    public override MemoryHandle Pin(int elementIndex = 0)
    {
        ObjectDisposedException.ThrowIf(this.memoryManager.IsDisposed, this.memoryManager);

        return elementIndex >= 0 && elementIndex < this.length
                   ? new MemoryHandle(this.pointer + elementIndex)
                   : throw new ArgumentOutOfRangeException(nameof(elementIndex), "Element index must be within memory range");
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">If the underlying memory has been disposed</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Unpin() => ObjectDisposedException.ThrowIf(this.memoryManager.IsDisposed, this.memoryManager);

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        this.memoryManager = null!;
        this.pointer = null;
    }
}
