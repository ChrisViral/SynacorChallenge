using System.Buffers;
using JetBrains.Annotations;

namespace Synacor.Data;

/// <summary>
/// Non-owning view over a <see cref="Memory"/>
/// </summary>
/// <typeparam name="T">View value type</typeparam>
[PublicAPI]
public sealed unsafe class MemoryView<T> : MemoryManager<T> where T : unmanaged
{
    private Memory memory;
    private T* pointer;
    private readonly int length;

    /// <summary>
    /// Creates a view over the given <see cref="Memory"/>
    /// </summary>
    /// <param name="memory">Memory block to create the view over</param>
    public MemoryView(Memory memory)
    {
        this.memory = memory;
        this.pointer = (T*)memory.Buffer;
        this.length = (int)memory.ByteLength / sizeof(T);
    }

    /// <summary>
    /// Creates a view at a given offset and length over the given <see cref="Memory"/>
    /// </summary>
    /// <param name="memory">Memory block to create the view over</param>
    /// <param name="offset">View offset, in <typeparamref name="T"/> size</param>
    /// <param name="length">View length, in <typeparamref name="T"/> size</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// If <paramref name="offset"/> is less than zero or greater than than the original block size,
    /// or if <paramref name="length"/> is les than zero or larger than the available memory size from the offset
    /// </exception>
    public MemoryView(Memory memory, int offset, int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(offset, (int)memory.ByteLength / sizeof(T));
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(length, ((int)memory.ByteLength / sizeof(T)) - offset);

        this.memory = memory;
        this.pointer = (T*)memory.Buffer + offset;
        this.length = length;
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">If the underlying memory has been disposed</exception>
    public override Span<T> GetSpan()
    {
        ObjectDisposedException.ThrowIf(this.memory.IsDisposed, this.memory);

        return new Span<T>(this.pointer, this.length);
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">If the underlying memory has been disposed</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="elementIndex"/> is outside of the range of the memory block</exception>
    public override MemoryHandle Pin(int elementIndex = 0)
    {
        ObjectDisposedException.ThrowIf(this.memory.IsDisposed, this.memory);

        return elementIndex >= 0 && elementIndex < this.length
                   ? new MemoryHandle(this.pointer + elementIndex)
                   : throw new ArgumentOutOfRangeException(nameof(elementIndex), "Element index must be within memory range");
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">If the underlying memory has been disposed</exception>
    public override void Unpin() => ObjectDisposedException.ThrowIf(this.memory.IsDisposed, this.memory);

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        this.memory  = null!;
        this.pointer = null;
    }
}
