using System.Buffers;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace Synacor.Data;

/// <summary>
/// <see cref="VirtualMachine"/> memory block
/// </summary>
[PublicAPI]
public sealed unsafe class Memory : MemoryManager<ushort>
{
    /// <summary>
    /// Memory block length
    /// </summary>
    public int Length { get; private set; }

    /// <summary>
    /// Memory block length in bytes
    /// </summary>
    public int ByteLength { get; private set; }

    /// <summary>
    /// Buffer start address
    /// </summary>
    /// <exception cref="ObjectDisposedException">If this <see cref="Memory"/> has been disposed</exception>
    public ushort* Buffer
    {
        get
        {
            ObjectDisposedException.ThrowIf(this.IsDisposed, this);
            return field;
        }
        private set;
    }

    /// <summary>
    /// If this memory block has been disposed
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Creates a new unmanaged memory block of the specified length
    /// </summary>
    /// <param name="length">Length of the memory block to create</param>
    /// <exception cref="ArgumentOutOfRangeException">If the length is less than zero</exception>
    public Memory(int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        this.Length     = length;
        this.ByteLength = length * sizeof(ushort);
        this.Buffer     = (ushort*)NativeMemory.AllocZeroed((nuint)this.ByteLength);
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">If this <see cref="Memory"/> has been disposed</exception>
    public override Span<ushort> GetSpan()
    {
        ObjectDisposedException.ThrowIf(this.IsDisposed, this);

        return new Span<ushort>(this.Buffer, this.Length);
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">If this <see cref="Memory"/> has been disposed</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="elementIndex"/> is outside of the range of the memory block</exception>
    public override MemoryHandle Pin(int elementIndex = 0)
    {
        ObjectDisposedException.ThrowIf(this.IsDisposed, this);

        return elementIndex >= 0 && elementIndex < this.Length
                   ? new MemoryHandle(this.Buffer + elementIndex)
                   : throw new ArgumentOutOfRangeException(nameof(elementIndex), "Element index must be within memory range");
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">If this <see cref="Memory"/> has been disposed</exception>
    public override void Unpin() => ObjectDisposedException.ThrowIf(this.IsDisposed, this);

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
