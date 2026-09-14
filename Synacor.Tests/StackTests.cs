using FluentAssertions;
using Synacor.Data;

namespace Synacor.Tests;

public sealed class StackTests
{
    private const ushort VALUES = 5;

    [Fact]
    public void Constructor_CreatesWithCorrectCapacity()
    {
        using Stack defaultStack = new();
        defaultStack.Capacity.Should().Be(Stack.MIN_CAPACITY);
        defaultStack.MinCapacity.Should().Be(Stack.MIN_CAPACITY);
        for (int i = Stack.MIN_CAPACITY / Stack.GROW_FACTOR; i <= Stack.MIN_CAPACITY * Stack.GROW_FACTOR; i *= Stack.GROW_FACTOR)
        {
            using Stack stack = new(i);
            stack.MinCapacity.Should().Be(Stack.MIN_CAPACITY);
            int expectedCapacity = Math.Max(i, stack.MinCapacity);
            stack.Capacity.Should().Be(expectedCapacity);
        }
    }

    [Fact]
    public unsafe void Push_AddsValuesCorrectly()
    {
        using Stack stack = new();
        stack.Count.Should().Be(0);
        stack.IsEmpty.Should().BeTrue();
        for (ushort i = 1; i <= VALUES; i++)
        {
            stack.Push(i);
            stack.Count.Should().Be(i);
        }

        stack.IsEmpty.Should().BeFalse();
        ushort* pointer = stack.top;
        for (ushort i = VALUES; i > 0; i--)
        {
            pointer--;
            (*pointer).Should().Be(i);
        }
    }

    [Fact]
    public void Pop_ReturnsPushedValues()
    {
        using Stack stack = new();
        for (ushort i = 1; i <= VALUES; i++)
        {
            stack.Push(i);
        }

        for (ushort i = VALUES; i > 0; i--)
        {
            ushort value = stack.Pop();
            value.Should().Be(i);
        }

        stack.Count.Should().Be(0);
    }

    [Fact]
    public void TryPop_ReturnsPushedValues()
    {
        using Stack stack = new();
        for (ushort i = 1; i <= VALUES; i++)
        {
            stack.Push(i);
        }

        for (ushort i = VALUES; i > 0; i--)
        {
            bool result = stack.TryPop(out ushort value);
            result.Should().BeTrue();
            value.Should().Be(i);
        }

        stack.Count.Should().Be(0);
    }

    [Fact]
    public void Peek_ReturnsTopElement()
    {
        using Stack stack = new();
        for (ushort i = 1; i <= VALUES; i++)
        {
            stack.Push(i);
            stack.Peek().Should().Be(i);
        }
    }

    [Fact]
    public void TryPeek_ReturnsTopElement()
    {
        using Stack stack = new();
        for (ushort i = 1; i <= VALUES; i++)
        {
            stack.Push(i);
            bool result = stack.TryPeek(out ushort value);
            result.Should().BeTrue();
            value.Should().Be(i);
        }
    }

    [Fact]
    public void Pop_Peek_ThrowsWhenEmpty()
    {
        using Stack stack = new();

        // ReSharper disable once AccessToDisposedClosure
        Action pop = () => stack.Pop();
        pop.Should().Throw<InvalidOperationException>();

        // ReSharper disable once AccessToDisposedClosure
        Action peek = () => stack.Peek();
        peek.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void TryPop_TryPeek_FailsWhenEmpty()
    {
        using Stack stack = new();

        stack.TryPop(out _).Should().BeFalse();
        stack.TryPeek(out _).Should().BeFalse();
    }

    [Fact]
    public void Contains_MatchesExistingItemsOnly()
    {
        using Stack stack = new();
        for (ushort i = 1; i <= VALUES; i++)
        {
            stack.Push(i);
        }

        for (ushort i = 1; i <= VALUES; i++)
        {
            stack.Contains(i).Should().BeTrue();
        }

        for (ushort i = VALUES + 1; i <= VALUES * 2; i++)
        {
            stack.Contains(i).Should().BeFalse();
        }
    }

    [Fact]
    public void CopyTo_CopiesValuesLIFO()
    {
        using Stack stack = new();
        for (ushort i = 1; i <= VALUES; i++)
        {
            stack.Push(i);
        }

        Span<ushort> values = stackalloc ushort[VALUES];
        stack.CopyTo(values);

        for (int i = 0; i < VALUES; i++)
        {
            values[i].Should().Be((ushort)(VALUES - i));
            values[i].Should().Be(stack.Pop());
        }
    }

    [Fact]
    public void ToArray_CopiesValuesLIFO()
    {
        using Stack stack = new();
        for (ushort i = 1; i <= VALUES; i++)
        {
            stack.Push(i);
        }

        // ReSharper disable once UseCollectionExpression
        ushort[] values = stack.ToArray();

        for (int i = 0; i < VALUES; i++)
        {
            values[i].Should().Be((ushort)(VALUES - i));
            values[i].Should().Be(stack.Pop());
        }
    }

    [Fact]
    public void EnsureCapacity_SetsCapacity()
    {
        using Stack stack = new();
        for (int i = Stack.MIN_CAPACITY / Stack.GROW_FACTOR; i <= Stack.MIN_CAPACITY * Stack.GROW_FACTOR; i *= Stack.GROW_FACTOR)
        {
            int expectedCapacity = Math.Max(i, stack.MinCapacity);
            int capacity = stack.EnsureCapacity(i);
            capacity.Should().Be(stack.Capacity);
            capacity.Should().Be(expectedCapacity);
        }
    }

    [Fact]
    public void EnsureCapacity_DoesNotShrink()
    {
        using Stack stack = new(Stack.MIN_CAPACITY * Stack.GROW_FACTOR);
        for (int i = Stack.MIN_CAPACITY / Stack.GROW_FACTOR; i <= Stack.MIN_CAPACITY * Stack.GROW_FACTOR; i *= Stack.GROW_FACTOR)
        {
            int capacity = stack.EnsureCapacity(i);
            capacity.Should().Be(Stack.MIN_CAPACITY * Stack.GROW_FACTOR);
        }
    }

    [Fact]
    public void TrimExcess_RemovesCapacity()
    {
        using Stack stack = new();
        stack.EnsureCapacity(Stack.MIN_CAPACITY * Stack.GROW_FACTOR);
        stack.TrimExcess();
        stack.Capacity.Should().Be(stack.MinCapacity);
        for (int i = Stack.MIN_CAPACITY / Stack.GROW_FACTOR; i <= Stack.MIN_CAPACITY * Stack.GROW_FACTOR; i *= Stack.GROW_FACTOR)
        {
            int expectedCapacity = Math.Max(i, stack.MinCapacity);
            stack.EnsureCapacity(i * Stack.GROW_FACTOR);
            stack.TrimExcess(i);
            stack.Capacity.Should().Be(expectedCapacity);
        }
    }

    [Fact]
    public void TrimExcess_DoesNotIncrease()
    {
        using Stack stack = new();
        for (int i = Stack.MIN_CAPACITY; i <= Stack.MIN_CAPACITY * Stack.GROW_FACTOR * Stack.GROW_FACTOR; i *= Stack.GROW_FACTOR)
        {
            stack.TrimExcess(i);
            stack.Capacity.Should().Be(Stack.MIN_CAPACITY);
        }
    }

    [Fact]
    public void TrimExcess_TrimsAtNinetyPercent()
    {
        const ushort CAPACITY = 200;
        const ushort NINETY = (CAPACITY * 9) / 10;
        using Stack stack = new(CAPACITY);
        for (ushort i = 1; i <= CAPACITY; i++)
        {
            stack.Push(i);
        }

        stack.Capacity.Should().Be(CAPACITY);
        while (stack.Count > NINETY + 1)
        {
            stack.Pop();
            stack.TrimExcess();
            stack.Capacity.Should().Be(CAPACITY);
        }

        stack.Pop();
        stack.TrimExcess();
        stack.Capacity.Should().Be(NINETY);
    }

    [Fact]
    public void Clear_EmptiesStack()
    {
        using Stack stack = new();
        for (ushort i = 1; i <= VALUES; i++)
        {
            stack.Push(i);
        }

        stack.Clear();
        stack.Count.Should().Be(0);
        stack.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Clear_ResetsCapacity()
    {
        using Stack stack = new(Stack.MIN_CAPACITY * 2);
        for (ushort i = 1; i <= VALUES; i++)
        {
            stack.Push(i);
        }

        stack.Clear(resetCapacity: true);
        stack.Capacity.Should().Be(stack.MinCapacity);
    }

    [Fact]
    public void RefEnumerator_EnumeratesLIFO()
    {
        using Stack stack = new(Stack.MIN_CAPACITY * 2);
        for (ushort i = 1; i <= VALUES; i++)
        {
            stack.Push(i);
        }

        ushort expectedValue = VALUES;
        foreach (ushort value in stack)
        {
            value.Should().Be(expectedValue);
            expectedValue--;
        }
    }

    [Fact]
    public void Enumerator_EnumeratesLIFO()
    {
        using Stack stack = new(Stack.MIN_CAPACITY * 2);
        for (ushort i = 1; i <= VALUES; i++)
        {
            stack.Push(i);
        }

        ushort expectedValue = VALUES;
        IEnumerable<ushort> enumerable = stack;
        foreach (ushort value in enumerable)
        {
            value.Should().Be(expectedValue);
            expectedValue--;
        }
    }

    [Fact]
    public void Push_AtCapacity_GrowsStack()
    {
        using Stack stack = new(Stack.MIN_CAPACITY);
        stack.Capacity.Should().Be(Stack.MIN_CAPACITY);
        for (ushort i = 1; i <= Stack.MIN_CAPACITY; i++)
        {
            stack.Push(i);
            stack.Capacity.Should().Be(Stack.MIN_CAPACITY);
        }

        stack.Push((ushort)(stack.Count + 1));
        stack.Capacity.Should().Be(Stack.MIN_CAPACITY * Stack.GROW_FACTOR);
        stack.Count.Should().Be(Stack.MIN_CAPACITY + 1);
    }

    [Fact]
    public void Push_KeepsDataAfterGrow()
    {
        using Stack stack = new(Stack.MIN_CAPACITY);
        for (ushort i = 1; i <= Stack.MIN_CAPACITY + 1; i++)
        {
            stack.Push(i);
        }

        ushort expected = Stack.MIN_CAPACITY + 1;
        foreach (ushort value in stack)
        {
            value.Should().Be(expected);
            expected--;
        }
    }

    [Fact]
    public void Pop_ShrinksWhenAllowed()
    {
        using Stack stack = new(Stack.MIN_CAPACITY);
        for (ushort i = 1; i <= Stack.MIN_CAPACITY + 1; i++)
        {
            stack.Push(i);
        }

        int newCapacity = stack.Capacity;
        int shrinkCount = newCapacity / (Stack.GROW_FACTOR * Stack.GROW_FACTOR);
        while (stack.Count > shrinkCount)
        {
            stack.Pop();
            stack.Capacity.Should().Be(newCapacity);
        }

        stack.Pop();
        stack.Capacity.Should().Be(newCapacity / Stack.GROW_FACTOR);
    }

    [Fact]
    public void Pop_DoesNotShrinkWhenNotAllowed()
    {
        using Stack stack = new(Stack.MIN_CAPACITY);
        for (ushort i = 1; i <= Stack.MIN_CAPACITY + 1; i++)
        {
            stack.Push(i);
        }

        int newCapacity = stack.Capacity;
        while (!stack.IsEmpty)
        {
            stack.Pop(allowShrink: false);
            stack.Capacity.Should().Be(newCapacity);
        }
    }

    [Fact]
    public void Pop_KeepsDataAfterShrink()
    {
        using Stack stack = new(Stack.MIN_CAPACITY);
        for (ushort i = 1; i <= Stack.MIN_CAPACITY + 1; i++)
        {
            stack.Push(i);
        }

        int newCapacity = stack.Capacity;
        int shrinkCount = newCapacity / (Stack.GROW_FACTOR * Stack.GROW_FACTOR);
        while (stack.Count > shrinkCount)
        {
            stack.Pop();
        }

        stack.Pop();
        stack.Capacity.Should().Be(newCapacity / Stack.GROW_FACTOR);

        ushort expectedValue = (ushort)shrinkCount;
        foreach (ushort value in stack)
        {
            value.Should().Be(expectedValue);
            expectedValue--;
        }
    }

    [Fact]
    public void TryPop_ShrinksWhenAllowed()
    {
        using Stack stack = new(Stack.MIN_CAPACITY);
        for (ushort i = 1; i <= Stack.MIN_CAPACITY + 1; i++)
        {
            stack.Push(i);
        }

        int newCapacity = stack.Capacity;
        int shrinkCount = newCapacity / (Stack.GROW_FACTOR * Stack.GROW_FACTOR);
        while (stack.Count > shrinkCount)
        {
            stack.TryPop(out _);
            stack.Capacity.Should().Be(newCapacity);
        }

        stack.TryPop(out _);
        stack.Capacity.Should().Be(newCapacity / Stack.GROW_FACTOR);
    }

    [Fact]
    public void TryPop_DoesNotShrinkWhenNotAllowed()
    {
        using Stack stack = new(Stack.MIN_CAPACITY);
        for (ushort i = 1; i <= Stack.MIN_CAPACITY + 1; i++)
        {
            stack.Push(i);
        }

        int newCapacity = stack.Capacity;
        while (!stack.IsEmpty)
        {
            stack.TryPop(out _, allowShrink: false);
            stack.Capacity.Should().Be(newCapacity);
        }
    }

    [Fact]
    public void TryPop_KeepsDataAfterShrink()
    {
        using Stack stack = new(Stack.MIN_CAPACITY);
        for (ushort i = 1; i <= Stack.MIN_CAPACITY + 1; i++)
        {
            stack.Push(i);
        }

        int newCapacity = stack.Capacity;
        int shrinkCount = newCapacity / (Stack.GROW_FACTOR * Stack.GROW_FACTOR);
        while (stack.Count > shrinkCount)
        {
            stack.TryPop(out _);
        }

        stack.TryPop(out _);
        stack.Capacity.Should().Be(newCapacity / Stack.GROW_FACTOR);

        ushort expectedValue = (ushort)shrinkCount;
        foreach (ushort value in stack)
        {
            value.Should().Be(expectedValue);
            expectedValue--;
        }
    }
}
