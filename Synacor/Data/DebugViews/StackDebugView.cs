using System.Diagnostics;

namespace Synacor.Data.DebugViews;

internal sealed class StackDebugView(Stack stack)
{
    private readonly Stack stack = stack;

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public Value[] Items => [..this.stack];
}
