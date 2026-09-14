using System.Buffers;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace Synacor.Data;

/// <summary>
/// <see cref="VirtualMachine"/> unmanaged memory block manager
/// </summary>
[PublicAPI]
public sealed unsafe class MemoryManager : MemoryManager<Value>
{
    /// <summary>
    /// Memory block length
    /// </summary>
    public int Length { get; private set; }

    /// <summary>
    /// Memory block length in bytes
    /// </summary>
    public nuint ByteLength { get; private set; }

    /// <summary>
    /// Buffer start address
    /// </summary>
    /// <exception cref="ObjectDisposedException">If this <see cref="MemoryManager"/> has been disposed</exception>
    public Value* Buffer
    {
        get
        {
            ObjectDisposedException.ThrowIf(this.IsDisposed, this);
            return field;
        }
        private set;
    }

    /// <summary>
    /// If this <see cref="MemoryManager"/> has been disposed
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Creates a new <see cref="MemoryManager"/> with an unmanaged memory block of the specified length
    /// </summary>
    /// <param name="length">Length of the memory block to create</param>
    /// <exception cref="ArgumentOutOfRangeException">If the length is less than zero</exception>
    public MemoryManager(int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        this.Length     = length;
        this.ByteLength = (nuint)length * Value.SIZE;
        this.Buffer     = (Value*)NativeMemory.AllocZeroed(this.ByteLength);
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">If this <see cref="MemoryManager"/> has been disposed</exception>
    public override Span<Value> GetSpan()
    {
        ObjectDisposedException.ThrowIf(this.IsDisposed, this);

        return new Span<Value>(this.Buffer, this.Length);
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">If this <see cref="MemoryManager"/> has been disposed</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="elementIndex"/> is outside of the range of the memory block</exception>
    public override MemoryHandle Pin(int elementIndex = 0)
    {
        ObjectDisposedException.ThrowIf(this.IsDisposed, this);

        return elementIndex >= 0 && elementIndex < this.Length
                   ? new MemoryHandle(this.Buffer + elementIndex)
                   : throw new ArgumentOutOfRangeException(nameof(elementIndex), "Element index must be within memory range");
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">If this <see cref="MemoryManager"/> has been disposed</exception>
    public override void Unpin() => ObjectDisposedException.ThrowIf(this.IsDisposed, this);

    /// <summary>
    /// Zeroes the entirety of this <see cref="MemoryManager"/>'s unmanaged memory block
    /// </summary>
    /// <exception cref="ObjectDisposedException">If this <see cref="MemoryManager"/> has been disposed</exception>
    public void Clear()
    {
        ObjectDisposedException.ThrowIf(this.IsDisposed, this);

        NativeMemory.Clear(this.Buffer, this.ByteLength);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (this.IsDisposed) return;

        NativeMemory.Free(this.Buffer);
        this.Length = 0;
        this.ByteLength = 0;
        this.Buffer = null;
        this.IsDisposed = true;
    }
}
