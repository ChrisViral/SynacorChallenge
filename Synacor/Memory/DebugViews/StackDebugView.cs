using System.Diagnostics;
using Synacor.Data;

namespace Synacor.Memory.DebugViews;

internal sealed class StackDebugView(Stack stack)
{
    private readonly Stack stack = stack;

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public Value[] Items => [..this.stack];
}
