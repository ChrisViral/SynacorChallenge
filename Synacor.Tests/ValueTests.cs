using FluentAssertions;
using Value = Synacor.Data.Value;

namespace Synacor.Tests;

/// <summary>
/// <see cref="Value"/> unit tests
/// </summary>
public class ValueTests
{
    private const int MODULUS = 32768;

    private static ushort Mod(int value) => (ushort)(((value % MODULUS) + MODULUS) % MODULUS);

    [Fact]
    public void Constructor_ShouldProduceCorrectValues()
    {
        for (ushort i = 0; i <= Value.MAX_REGISTER; i++)
        {
            Value value = new(i);
            value.Raw.Should().Be(i);
        }
    }

    [Fact]
    public void Cast_ShouldProduceCorrectValues()
    {
        for (ushort i = 0; i <= Value.MAX_REGISTER; i++)
        {
            Value value = i;
            value.Raw.Should().Be(i);
        }
    }

    [Fact]
    public void Uncast_ShouldProduceCorrectValues()
    {
        for (ushort i = 0; i <= Value.MAX_REGISTER; i++)
        {
            Value value = i;
            ushort original = value;
            original.Should().Be(i);
        }
    }

    [Fact]
    public void Constructor_ShouldThrowOnInvalidValues()
    {
        for (int i = Value.MAX_REGISTER + 1; i <= ushort.MaxValue; i++)
        {
            ushort asShort = (ushort)i;
            Action test = () => _ = new Value(asShort);
            test.Should().Throw<ArgumentOutOfRangeException>();
        }
    }

    [Fact]
    public void Numbers_ShouldHaveValidFlags()
    {
        for (ushort i = 0; i <= Value.MAX_VALUE; i++)
        {
            Value value = i;
            value.IsNumber.Should().BeTrue();
            value.IsRegister.Should().BeFalse();
        }
    }

    [Fact]
    public void Registers_ShouldHaveValidFlags()
    {
        for (ushort i = Value.MAX_VALUE + 1; i <= Value.MAX_REGISTER; i++)
        {
            Value value = i;
            value.IsNumber.Should().BeFalse();
            value.IsRegister.Should().BeTrue();
        }
    }

    [Fact]
    public void RegisterAddress_ShouldThrowForNumbers()
    {
        for (ushort i = 0; i <= Value.MAX_VALUE; i++)
        {
            Value value = i;
            Action test = () => _ = value.RegisterAddress;
            test.Should().Throw<InvalidOperationException>();
        }
    }

    [Fact]
    public void RegisterAddress_ShouldGiveCorrectAddressForRegisters()
    {
        for (int i = 0; i < Value.REGISTER_COUNT; i++)
        {
            Value value = (ushort)(i + Value.MAX_VALUE + 1);
            value.RegisterAddress.Should().Be(i);
        }
    }

    [Theory]
    [InlineData(0b00000000_00000001, 1,  0b00000000_00000010)]
    [InlineData(0b00000000_10000000, 1,  0b00000001_00000000)]
    [InlineData(0b01000000_00000000, 1,  0b00000000_00000001)]
    [InlineData(0b01010101_01010101, 1,  0b00101010_10101011)]
    [InlineData(0b00000000_00000001, 8,  0b00000001_00000000)]
    [InlineData(0b00000000_00000001, 14, 0b01000000_00000000)]
    [InlineData(0b01100111_00111101, 15, 0b01100111_00111101)]
    [InlineData(0b00000000_00000001, 16, 0b00000000_00000010)]
    public void RotateLeft_ShouldRotateBits(ushort value, int rotateAmount, ushort expected)
    {
        Value.RotateLeft(value, rotateAmount).Should().Be(expected);
    }

    [Theory]
    [InlineData(0b00000000_00000010, 1,  0b00000000_00000001)]
    [InlineData(0b00000001_00000000, 1,  0b00000000_10000000)]
    [InlineData(0b00000000_00000001, 1,  0b01000000_00000000)]
    [InlineData(0b01010101_01010101, 1,  0b01101010_10101010)]
    [InlineData(0b00000001_00000000, 8,  0b00000000_00000001)]
    [InlineData(0b01000000_00000000, 14, 0b00000000_00000001)]
    [InlineData(0b01100111_00111101, 15, 0b01100111_00111101)]
    [InlineData(0b00000000_00000010, 16, 0b00000000_00000001)]
    public void RotateRight_ShouldRotateBits(ushort value, int rotateAmount, ushort expected)
    {
        Value.RotateRight(value, rotateAmount).Should().Be(expected);
    }

    [Theory]
    [InlineData(0b00000000_00000000, 0b00000000_00000000)]
    [InlineData(0b00000000_00000001, 0b01111111_11111111)]
    [InlineData(0b01111111_11111111, 0b00000000_00000001)]
    [InlineData(0b00000001_00000000, 0b01111111_00000000)]
    [InlineData(0b01010101_01010101, 0b00101010_10101011)]
    public void Negation_ShouldProduceCorrectResult(ushort value, ushort expected)
    {
        Value start = value;
        Value negated = -start;
        negated.Should().Be(expected);
    }

    [Theory]
    [InlineData(2, 1)]
    [InlineData(17, 11)]
    [InlineData(63, 43)]
    [InlineData(1001, 29)]
    public void MathematicalOperations_ProduceCorrectResult(ushort a, ushort b)
    {
        Value x = a;
        Value y = b;
        (x + y).Should().Be((ushort)(a + b));
        (x - y).Should().Be((ushort)(a - b));
        (x * y).Should().Be((ushort)(a * b));
        (x / y).Should().Be((ushort)(a / b));
        (x % y).Should().Be((ushort)(a % b));
    }

    [Theory]
    [InlineData(1, Value.MAX_VALUE)]
    [InlineData(2, Value.MAX_VALUE)]
    [InlineData(12345, 23456)]
    [InlineData(9999, 9999)]
    public void MathematicalOperations_Overflow_ShouldRespectModulus(ushort a, ushort b)
    {
        Value x = a;
        Value y = b;
        (x + y).Should().Be(Mod(a + b));
        (x - y).Should().Be(Mod(a - b));
        (x * y).Should().Be(Mod(a * b));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(Value.MAX_VALUE, 1)]
    [InlineData(12345, 23456)]
    [InlineData(9999, 9999)]
    [InlineData(15688, 32156)]
    public void BitwiseOperations_ProduceCorrectResult(ushort a, ushort b)
    {
        Value x = a;
        Value y = b;
        (~x).Should().Be(Mod(~a));
        (x & y).Should().Be(Mod(a & b));
        (x | y).Should().Be(Mod(a | b));
        (x ^ y).Should().Be(Mod(a ^ b));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(0, 10)]
    [InlineData(Value.MAX_VALUE, 1)]
    [InlineData(Value.MAX_VALUE, 10)]
    [InlineData(Value.MAX_VALUE, 15)]
    [InlineData(Value.MAX_VALUE, 20)]
    [InlineData(12345, 3)]
    [InlineData(9999, 7)]
    [InlineData(15688, 10)]
    public void ShiftOperations_ProduceCorrectResult(ushort a, int b)
    {
        Value x = a;
        (x << b).Should().Be(Mod(a << b));
        (x >> b).Should().Be(Mod(a >> b));
        (x >>> b).Should().Be(Mod(a >>> b));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(Value.MAX_VALUE, 1)]
    [InlineData(12345, 23456)]
    [InlineData(9999, 9999)]
    [InlineData(15688, 32156)]
    [InlineData(Value.MAX_VALUE + 2, Value.MAX_VALUE + 2)]
    [InlineData(Value.MAX_VALUE + 3, Value.MAX_VALUE + 1)]
    public void RelationalOperations_ProduceCorrectResult(ushort a, ushort b)
    {
        Value x = a;
        Value y = b;
        (x == y).Should().Be(a == b);
        (x != y).Should().Be(a != b);
        (x < y).Should().Be(a < b);
        (x <= y).Should().Be(a <= b);
        (x > y).Should().Be(a > b);
        (x >= y).Should().Be(a >= b);
        x.Equals(y).Should().Be(a.Equals(b));
        x.Equals((object)y).Should().Be(a.Equals((object)b));
        x.CompareTo(y).Should().Be(a.CompareTo(b));
        x.CompareTo((object)y).Should().Be(a.CompareTo((object)b));
    }

    [Fact]
    public void MathOperations_Registers_ShouldThrow()
    {
        Value a = 0;
        for (ushort y = Value.MAX_VALUE + 1; y <= Value.MAX_REGISTER; y++)
        {
            Value b = y;
            Action positive = () => _ = +b;
            positive.Should().Throw<InvalidOperationException>();
            Action negate = () => _ = -b;
            negate.Should().Throw<InvalidOperationException>();
            Action add = () => _ = a + b;
            add.Should().Throw<InvalidOperationException>();
            Action subtract = () => _ = a - b;
            subtract.Should().Throw<InvalidOperationException>();
            Action multiply = () => _ = a * b;
            multiply.Should().Throw<InvalidOperationException>();
            Action divide = () => _ = a / b;
            divide.Should().Throw<InvalidOperationException>();
            Action modulus = () => _ = a % b;
            modulus.Should().Throw<InvalidOperationException>();
        }
    }

    [Fact]
    public void BitwiseOperations_Registers_ShouldThrow()
    {
        Value a = 0;
        for (ushort y = Value.MAX_VALUE + 1; y <= Value.MAX_REGISTER; y++)
        {
            Value b = y;
            Action invert = () => _ = ~b;
            invert.Should().Throw<InvalidOperationException>();
            Action and = () => _ = a & b;
            and.Should().Throw<InvalidOperationException>();
            Action or = () => _ = a | b;
            or.Should().Throw<InvalidOperationException>();
            Action xor = () => _ = a ^ b;
            xor.Should().Throw<InvalidOperationException>();
        }
    }

    [Fact]
    public void ShiftOperations_Registers_ShouldThrow()
    {
        for (ushort x = Value.MAX_VALUE + 1; x <= Value.MAX_REGISTER; x++)
        {
            Value a = x;
            Action left = () => _ = a << 1;
            left.Should().Throw<InvalidOperationException>();
            Action right = () => _ = a >> 1;
            right.Should().Throw<InvalidOperationException>();
            Action rightSigned = () => _ = a >>> 1;
            rightSigned.Should().Throw<InvalidOperationException>();
        }
    }
}
