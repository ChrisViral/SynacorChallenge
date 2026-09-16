using System.Buffers;
using System.Diagnostics;
using Synacor.Data;

namespace Synacor.Memory.DebugViews;

internal class MemoryManagerDebugView<T>(MemoryManager<T> manager)
{
    private readonly MemoryManager<T> manager = manager;

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public T[] Items => this.manager.Memory.ToArray();
}

internal sealed class MemoryManagerDebugView(MemoryManager manager) : MemoryManagerDebugView<Value>(manager);

internal sealed class StackViewDebugView(StackView stack) : MemoryManagerDebugView<byte>(stack);
