using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Synacor.Data;
using Synacor.Memory.DebugViews;

namespace Synacor.Memory;

/// <summary>
/// <see cref="Stack"/> memory view
/// </summary>
/// <param name="stack">Stack to create the view over</param>
[DebuggerDisplay("Size = {stack.Count}"), DebuggerTypeProxy(typeof(StackViewDebugView))]
internal sealed unsafe class StackView(Stack stack) : MemoryManager<byte>
{
    private Stack stack = stack;

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">If the underlying memory has been disposed</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override Span<byte> GetSpan()
    {
        ObjectDisposedException.ThrowIf(this.stack.IsDisposed, this.stack);

        return new Span<byte>(this.stack.stack, this.stack.Count * Value.SIZE);
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">If the underlying memory has been disposed</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="elementIndex"/> is outside of the range of the memory block</exception>
    public override MemoryHandle Pin(int elementIndex = 0)
    {
        ObjectDisposedException.ThrowIf(this.stack.IsDisposed, this.stack);

        return elementIndex >= 0 && elementIndex < this.stack.Count * Value.SIZE
                   ? new MemoryHandle(this.stack.stack + (elementIndex * Value.SIZE))
                   : throw new ArgumentOutOfRangeException(nameof(elementIndex), "Element index must be within memory range");
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">If the underlying memory has been disposed</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Unpin() => ObjectDisposedException.ThrowIf(this.stack.IsDisposed, this.stack);

    /// <inheritdoc />
    protected override void Dispose(bool disposing) => this.stack = null!;
}
