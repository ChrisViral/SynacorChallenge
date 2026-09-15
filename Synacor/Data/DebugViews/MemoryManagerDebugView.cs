using System.Buffers;
using System.Diagnostics;

namespace Synacor.Data.DebugViews;

internal class MemoryManagerDebugView<T>(MemoryManager<T> manager)
{
    private readonly MemoryManager<T> manager = manager;

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public T[] Items => this.manager.Memory.ToArray();
}

internal sealed class MemoryManagerDebugView(MemoryManager manager) : MemoryManagerDebugView<Value>(manager);
