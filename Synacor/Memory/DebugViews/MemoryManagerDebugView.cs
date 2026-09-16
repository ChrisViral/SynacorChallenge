using System.Buffers;
using System.Diagnostics;
using JetBrains.Annotations;
using Synacor.Data;

namespace Synacor.Memory.DebugViews;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal class MemoryManagerDebugView<T>(MemoryManager<T> manager)
{
    private readonly MemoryManager<T> manager = manager;

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public T[] Items => this.manager.Memory.ToArray();
}

internal sealed class MemoryManagerDebugView(MemoryManager manager) : MemoryManagerDebugView<Value>(manager);

internal sealed class MemoryViewDebugView(MemoryView view) : MemoryManagerDebugView<byte>(view);

internal sealed class StackViewDebugView(StackView stack) : MemoryManagerDebugView<byte>(stack);
