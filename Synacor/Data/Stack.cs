using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Synacor.Data.DebugViews;

namespace Synacor.Data;

/// <summary>
/// Unmanaged memory resizable stack
/// </summary>
[PublicAPI, DebuggerDisplay("Count = {Count}"), DebuggerTypeProxy(typeof(StackDebugView))]
public sealed unsafe class Stack : IReadOnlyCollection<Value>, IDisposable
{
    /// <summary>
    /// Default and minimum <see cref="Stack"/> size
    /// </summary>
    internal const int MIN_CAPACITY = 128;
    /// <summary>
    /// Factor to grow the <see cref="Stack"/> by when full
    /// </summary>
    internal const int GROW_FACTOR = 2;

    internal Value* stack;
    internal Value* top;
    private int version;

    /// <summary>
    /// Current <see cref="Stack"/> size
    /// </summary>
    /// <exception cref="ObjectDisposedException">If this <see cref="Stack"/> has been disposed</exception>
    public int Count { get; private set; }

    /// <summary>
    /// The current capacity of the <see cref="Stack"/>
    /// </summary>
    public int Capacity
    {
        get;
        private set
        {
            if (field == value) return;

            if (!this.IsDisposed)
            {
                int count = this.Count;
                this.stack = (Value*)NativeMemory.Realloc(this.stack, (nuint)value * Value.SIZE);
                this.top = this.stack + count;
                this.version++;
            }
            field = value;
        }
    }

    /// <summary>
    /// Minimal capacity at which this stack is allowed to shrink
    /// </summary>
    public int MinCapacity
    {
        get;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => field = Math.Max(value, MIN_CAPACITY);
    } = MIN_CAPACITY;

    /// <summary>
    /// If this <see cref="Stack"/> has been disposed
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// If this <see cref="Stack"/> is currently empty
    /// </summary>
    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.Count is 0;
    }

    /// <summary>
    /// If this <see cref="Stack"/> is currently at full capacity
    /// </summary>
    public bool IsFull
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.Count == this.Capacity;
    }

    /// <summary>
    /// Creates a new <see cref="Stack"/> of default capacity
    /// </summary>
    public Stack() : this(MIN_CAPACITY) { }

    /// <summary>
    /// Creates a new <see cref="Stack"/> with the specified capacity
    /// </summary>
    /// <param name="capacity">Initial stack capacity</param>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="capacity"/> is less than zero</exception>
    public Stack(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

        this.Capacity = Math.Max(capacity, MIN_CAPACITY);
        this.stack = (Value*)NativeMemory.Alloc((nuint)capacity * Value.SIZE);
        this.top   = this.stack;
    }

    /// <summary>
    /// Deallocates unmanaged memory before being collected
    /// </summary>
    ~Stack() => ReleaseUnmanagedResources();

    /// <summary>
    /// Push an item onto the <see cref="Stack"/>
    /// </summary>
    /// <param name="item">Item to push</param>
    /// <exception cref="ObjectDisposedException">If this <see cref="Stack"/> has been disposed</exception>
    public void Push(Value item)
    {
        ObjectDisposedException.ThrowIf(this.IsDisposed, this);

        if (this.IsFull)
        {
            Grow();
        }

        *this.top = item;
        this.top++;
        this.Count++;
        this.version++;
    }

    /// <summary>
    /// Pop the item at the top of the <see cref="Stack"/>
    /// </summary>
    /// <param name="allowShrink">If the <see cref="Stack"/> should be allowed to shrink after the value is popped</param>
    /// <returns>The popped item</returns>
    /// <exception cref="ObjectDisposedException">If this <see cref="Stack"/> has been disposed</exception>
    /// <exception cref="InvalidOperationException">If the <see cref="Stack"/> is empty</exception>
    public Value Pop(bool allowShrink = true)
    {
        ObjectDisposedException.ThrowIf(this.IsDisposed, this);
        if (this.IsEmpty) throw new InvalidOperationException("Stack is empty, cannot pop a value");

        this.top--;
        this.Count--;
        this.version++;
        if (allowShrink)
        {
            ShrinkIfNeeded();
        }
        return *this.top;
    }

    /// <summary>
    /// Tries to pop the item at the top of this <see cref="Stack"/>
    /// </summary>
    /// <param name="item">Popped item if successful</param>
    /// <param name="allowShrink">If the <see cref="Stack"/> should be allowed to shrink after the value is popped</param>
    /// <returns><see langword="true"/> if the <see cref="Stack"/> was popped, othwerise <see langword="false"/></returns>
    /// <exception cref="ObjectDisposedException">If this <see cref="Stack"/> has been disposed</exception>
    public bool TryPop(out Value item, bool allowShrink = true)
    {
        ObjectDisposedException.ThrowIf(this.IsDisposed, this);

        if (this.IsEmpty)
        {
            item = 0;
            return false;
        }

        this.top--;
        this.Count--;
        this.version++;
        if (allowShrink)
        {
            ShrinkIfNeeded();
        }

        item = *this.top;
        return true;
    }

    /// <summary>
    /// Peeks the item at the top of the <see cref="Stack"/>
    /// </summary>
    /// <returns>The peeked item</returns>
    /// <exception cref="ObjectDisposedException">If this <see cref="Stack"/> has been disposed</exception>
    /// <exception cref="InvalidOperationException">If the <see cref="Stack"/> is empty</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Peek()
    {
        ObjectDisposedException.ThrowIf(this.IsDisposed, this);
        if (this.IsEmpty) throw new InvalidOperationException("Stack is empty, cannot peek top value");

        return *(this.top - 1);
    }

    /// <summary>
    /// Tries to peek the item at the top of the <see cref="Stack"/>
    /// </summary>
    /// <param name="item">Peeked item if successful</param>
    /// <returns><see langword="true"/> if the <see cref="Stack"/> was popped, othwerise <see langword="false"/></returns>
    /// <exception cref="ObjectDisposedException">If this <see cref="Stack"/> has been disposed</exception>
    public bool TryPeek(out Value item)
    {
        ObjectDisposedException.ThrowIf(this.IsDisposed, this);

        if (this.IsEmpty)
        {
            item = 0;
            return false;
        }

        item = *(this.top - 1);
        return true;
    }

    /// <summary>
    /// Checks if this <see cref="Stack"/> contains the given item
    /// </summary>
    /// <param name="item">Item to find</param>
    /// <returns><see langword="true"/> if <paramref name="item"/> was found in this <see cref="Stack"/>, otherwise <see langword="false"/></returns>
    /// <exception cref="ObjectDisposedException">If this <see cref="Stack"/> has been disposed</exception>
    public bool Contains(Value item)
    {
        ObjectDisposedException.ThrowIf(this.IsDisposed, this);

        for (Value* pointer = this.top; pointer != this.stack; /* pointer-- */)
        {
            pointer--;
            if (*pointer == item)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Copies the data in this <see cref="Stack"/> to a span
    /// </summary>
    /// <param name="destination">Destination span to copy to</param>
    /// <exception cref="ObjectDisposedException">If this <see cref="Stack"/> has been disposed</exception>
    /// <exception cref="ArgumentException">If <paramref name="destination"/> is too small to copy this <see cref="Stack"/>'s data into</exception>
    public void CopyTo(Span<Value> destination)
    {
        ObjectDisposedException.ThrowIf(this.IsDisposed, this);
        if (destination.Length < this.Count) throw new ArgumentException("Destination span too short to copy this stack", nameof(destination));

        Value* pointer = this.top;
        for (int i = 0; pointer != this.stack; i++)
        {
            pointer--;
            destination[i] = *pointer;
        }
    }

    /// <summary>
    /// Copies the data in this <see cref="Stack"/> to a new array and returns it
    /// </summary>
    /// <returns>A new array containing a copy of this <see cref="Stack"/></returns>
    /// <exception cref="ObjectDisposedException">If this <see cref="Stack"/> has been disposed</exception>
    public Value[] ToArray()
    {
        ObjectDisposedException.ThrowIf(this.IsDisposed, this);

        Value[] array = new Value[this.Count];
        Value* pointer = this.top;
        for (int i = 0; pointer != this.stack; i++)
        {
            pointer--;
            array[i] = *pointer;
        }
        return array;
    }

    /// <summary>
    /// Clears the data in this <see cref="Stack"/>
    /// </summary>
    /// <param name="resetCapacity">If this <see cref="Stack"/>'s capacity should be reset to <see cref="MinCapacity"/></param>
    /// <exception cref="ObjectDisposedException">If this <see cref="Stack"/> has been disposed</exception>
    public void Clear(bool resetCapacity = false)
    {
        ObjectDisposedException.ThrowIf(this.IsDisposed, this);

        // Reset the top pointer and stack size
        this.top = this.stack;
        this.Count = 0;
        this.version++;

        // Check if a new capacity has been requested
        if (resetCapacity)
        {
            this.Capacity = this.MinCapacity;
        }
    }

    /// <summary>
    /// Ensures that this <see cref="Stack"/>'s capacity is at least the given amount
    /// </summary>
    /// <param name="capacity">Minimum capacity to ensure</param>
    /// <returns>The new capacity of this <see cref="Stack"/></returns>
    /// <exception cref="ObjectDisposedException">If this <see cref="Stack"/> has been disposed</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="capacity"/> is less than zero</exception>
    public int EnsureCapacity(int capacity)
    {
        ObjectDisposedException.ThrowIf(this.IsDisposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

        if (this.Capacity < capacity)
        {
            this.Capacity = capacity;
        }

        return this.Capacity;
    }

    /// <summary>
    /// Reduces the capacity of this <see cref="Stack"/> to it's actual size if the current count of items is under 90 percent of the capacity.<br/>
    /// The capacity will never be set below <see cref="MinCapacity"/>
    /// </summary>
    /// <exception cref="ObjectDisposedException">If this <see cref="Stack"/> has been disposed</exception>
    public void TrimExcess()
    {
        ObjectDisposedException.ThrowIf(this.IsDisposed, this);

        int threshold = (this.Capacity * 9) / 10;
        if (this.Count <= threshold)
        {
            this.Capacity = Math.Max(this.MinCapacity, this.Count);
        }
    }

    /// <summary>
    /// Reduices the capacity of this <see cref="Stack"/> to the specified number<br/>
    /// The capacity will never be set below <see cref="MinCapacity"/>
    /// </summary>
    /// <exception cref="ObjectDisposedException">If this <see cref="Stack"/> has been disposed</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="newCapacity"/> is less than zero or less than <see cref="Count"/></exception>
    public void TrimExcess(int newCapacity)
    {
        ObjectDisposedException.ThrowIf(this.IsDisposed, this);

        if (newCapacity >= this.Capacity) return;
        ArgumentOutOfRangeException.ThrowIfNegative(newCapacity);
        ArgumentOutOfRangeException.ThrowIfLessThan(newCapacity, this.Count);

        newCapacity = Math.Max(newCapacity, this.MinCapacity);
        if (this.Capacity != newCapacity)
        {
            this.Capacity = newCapacity;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (this.IsDisposed) return;

        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
        this.IsDisposed = true;
        this.Capacity = 0;
        this.Count = 0;
    }

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator()" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StackRefEnumerator GetEnumerator() => new(this);

    /// <summary>
    /// Grows this <see cref="Stack"/> and reallocates memory
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Grow() => this.Capacity *= GROW_FACTOR;

    /// <summary>
    /// Shrinks this <see cref="Stack"/> when deemed necessary
    /// </summary>
    private void ShrinkIfNeeded()
    {
        // If we are over the default capacity's firth growth, and if the count is under two growths
        if (this.Capacity >= this.MinCapacity * GROW_FACTOR
         && this.Count < this.Capacity / (GROW_FACTOR * GROW_FACTOR))
        {
            this.Capacity /= GROW_FACTOR;
        }
    }

    /// <summary>
    /// Deallocates unmanaged memory
    /// </summary>
    private void ReleaseUnmanagedResources()
    {
        NativeMemory.Free(this.stack);
        this.stack = null;
        this.top   = null;
        this.version++;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator<Value> IEnumerable<Value>.GetEnumerator() => new StackEnumerator(this);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator IEnumerable.GetEnumerator() => new StackEnumerator(this);

    /// <summary>
    /// <see cref="Stack"/> ref enumerator
    /// </summary>
    /// <param name="stack">Stack to enumerate</param>
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public ref struct StackRefEnumerator(Stack stack)
    {
        private readonly Stack stack = stack;
        private readonly int version = stack.version;
        private Value* pointer;

        /// <inheritdoc cref="IEnumerator{T}.Current" />
        public Value Current { get; private set; }

        /// <inheritdoc cref="IEnumerator.MoveNext" />
        /// <exception cref="ObjectDisposedException">If the <see cref="Stack"/> this enumerates has been disposed</exception>
        public bool MoveNext()
        {
            ObjectDisposedException.ThrowIf(this.stack.IsDisposed, this.stack);
            if (this.version != this.stack.version) throw new InvalidOperationException("Stack modified during iteration");

            if (this.pointer > this.stack.stack)
            {
                this.pointer--;
                this.Current = *this.pointer;
                return true;
            }

            this.pointer = null;
            this.Current = 0;
            return false;
        }
    }

    /// <summary>
    /// <see cref="Stack"/> enumerator
    /// </summary>
    /// <param name="stack">Stack to enumerate</param>
    public sealed class StackEnumerator(Stack stack) : IEnumerator<Value>
    {
        private readonly Stack stack = stack;
        private readonly int version = stack.version;
        private Value* pointer = stack.top;

        /// <inheritdoc />
        public Value Current { get; private set; }

        /// <inheritdoc />
        /// <exception cref="ObjectDisposedException">If the <see cref="Stack"/> this enumerates has been disposed</exception>
        public bool MoveNext()
        {
            ObjectDisposedException.ThrowIf(this.stack.IsDisposed, this.stack);
            if (this.version != this.stack.version) throw new InvalidOperationException("Stack modified during iteration");

            if (this.pointer > this.stack.stack)
            {
                this.pointer--;
                this.Current = *this.pointer;
                return true;
            }

            this.pointer = null;
            this.Current = 0;
            return false;
        }

        /// <inheritdoc />
        /// <exception cref="ObjectDisposedException">If the <see cref="Stack"/> this enumerates has been disposed</exception>
        public void Reset()
        {
            ObjectDisposedException.ThrowIf(this.stack.IsDisposed, this.stack);
            if (this.version != this.stack.version) throw new InvalidOperationException("Stack modified during iteration");

            this.pointer = this.stack.top;
        }

        /// <inheritdoc />
        object IEnumerator.Current => this.Current;

        /// <inheritdoc />
        void IDisposable.Dispose() { }
    }
}
