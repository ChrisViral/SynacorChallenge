using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using FastEnumUtility;
using JetBrains.Annotations;

namespace Synacor.Data;

/// <summary>
/// Numerical or register value
/// </summary>
[PublicAPI]
public readonly struct Value : IBinaryInteger<Value>, IUnsignedNumber<Value>, IMinMaxValue<Value>, IConvertible
{
    /// <summary>
    /// The size of <see cref="Value"/> in bytes
    /// </summary>
    public const int SIZE = sizeof(ushort);
    /// <summary>
    /// The maximum numerical value stored in a <see cref="Value"/>
    /// </summary>
    public const ushort MAX_VALUE = (ushort)short.MaxValue;
    /// <summary>
    /// The amount of registers there are
    /// </summary>
    public const ushort REGISTER_COUNT = 8;
    /// <summary>
    /// The maximum register value stored in a <see cref="Value"/>
    /// </summary>
    public const ushort MAX_REGISTER = MAX_VALUE + REGISTER_COUNT;
    /// <summary>
    /// Bit count of the numerical values
    /// </summary>
    public const int BIT_COUNT = (sizeof(ushort) * 8) - 1;
    /// <summary>
    /// Mathematical operation mask (modulo 32768 equivalent)
    /// </summary>
    private const int MASK = 0x7FFF;

    /// <summary>
    /// <see cref="Value"/> representing <see langword="true"/>
    /// </summary>
    public static readonly Value True = 1;

    /// <summary>
    /// <see cref="Value"/> representing <see langword="false"/>
    /// </summary>
    public static readonly Value False = 0;

    /// <summary> Numerical value </summary>
    private readonly ushort value;

    /// <summary>
    /// Raw numerical value of this <see cref="Value"/>
    /// </summary>
    /// ReSharper disable once ConvertToAutoPropertyWhenPossible
    public ushort Raw
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.value;
    }

    /// <summary>
    /// If this <see cref="Value"/> contains a number
    /// </summary>
    public bool IsNumber
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.value <= MAX_VALUE;
    }

    /// <summary>
    /// If this <see cref="Value"/> contains a register address
    /// </summary>
    public bool IsRegister
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.value > MAX_VALUE;
    }

    /// <summary>
    /// If this <see cref="Value"/> is a valid <see cref="Opcode"/> value
    /// </summary>
    public bool IsOpcode
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => FastEnum.IsDefined((Opcode)this.value);
    }

    /// <summary>
    /// The register address of this <see cref="Value"/>
    /// </summary>
    /// <exception cref="InvalidOperationException">If this <see cref="Value"/> is not a register address</exception>
    public int RegisterAddress
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ThrowIfNumber();

            return this.value - MAX_VALUE - 1;
        }
    }

    /// <summary>
    /// Register address character for this <see cref="Value"/>
    /// </summary>
    private char RegisterChar
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
#if DEBUG
            ThrowIfNumber();
#endif

            return (char)(this.value + ('a' - MAX_VALUE));
        }
    }

    /// <summary>
    /// Creates a new <see cref="Value"/>
    /// </summary>
    /// <param name="value">The numerical value to initialize this to</param>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="value"/> is greater than <see cref="MAX_REGISTER"/></exception>
    public Value(ushort value)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MAX_REGISTER);

        this.value = value;
    }

    /// <summary>
    /// Creates a new <see cref="Value"/>
    /// </summary>
    /// <param name="value">The numerical value to initialize this to</param>
    private Value(int value)
    {
#if DEBUG
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MAX_REGISTER);
#endif

        this.value = (ushort)value;
    }


    // === Instance Methods ===


    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Value other) => this.value == other.value;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is Value other && Equals(other);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => this.value;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(Value other) => this.value.CompareTo(other.value);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(object? obj) => obj switch
    {
        null        => 1,
        Value other => CompareTo(other),
        _           => throw new ArgumentException("Object must be a Value", nameof(obj))
    };

    /// <summary>
    /// Converts this <see cref="Value"/> to either it's numberical string or register address string
    /// </summary>
    /// <returns>The string representation of this <see cref="Value"/></returns>
    public override string ToString()
    {
        if (this.IsRegister) return this.RegisterChar.ToString();
        return this.IsOpcode
                   ? $"({((Opcode)this.value).FastToString()}) {this.value}"
                   : this.value.ToString();
    }

    /// <inheritdoc />
    public string ToString(IFormatProvider? provider)
    {
        if (this.IsRegister) return this.RegisterChar.ToString(provider);
        return this.IsOpcode
                   ? $"({((Opcode)this.value).FastToString()}) {this.value.ToString(provider)}"
                   : this.value.ToString(provider);
    }

    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        if (this.IsRegister) return this.RegisterChar.ToString(formatProvider);
        return this.IsOpcode
                   ? $"({((Opcode)this.value).FastToString()}) {this.value.ToString(format, formatProvider)}"
                   : this.value.ToString(format, formatProvider);
    }

    /// <inheritdoc />
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (this.IsNumber) return this.value.TryFormat(destination, out charsWritten, format, provider);

        if (!destination.IsEmpty)
        {
            destination[0] = this.RegisterChar;
            charsWritten = 1;
            return true;
        }

        charsWritten = 0;
        return false;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TypeCode GetTypeCode() => this.value.GetTypeCode();

    /// <summary>
    /// Ensures this <see cref="Value"/> is not a numerical value
    /// </summary>
    /// <exception cref="InvalidOperationException">When <see cref="IsNumber"/> is <see langword="true"/></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ThrowIfNumber()
    {
        if (this.IsNumber)
        {
            throw new InvalidOperationException("This operation is not valid on numerical values");
        }
    }

    /// <summary>
    /// Ensures this <see cref="Value"/> is not a register address
    /// </summary>
    /// <exception cref="InvalidOperationException">When <see cref="IsRegister"/> is <see langword="true"/></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ThrowIfRegister()
    {
        if (this.IsRegister)
        {
            throw new InvalidOperationException("This operation is not valid on register values");
        }
    }


    // === Static Methods ===


    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">If the result is greater than <see cref="MAX_REGISTER"/></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value Parse(string s, IFormatProvider? provider) => ushort.Parse(s, provider);

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">If the result is greater than <see cref="MAX_REGISTER"/></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => ushort.Parse(s, provider);

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">If the result is greater than <see cref="MAX_REGISTER"/></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) => ushort.Parse(s, style, provider);

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">If the result is greater than <see cref="MAX_REGISTER"/></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value Parse(string s, NumberStyles style, IFormatProvider? provider) => ushort.Parse(s, style, provider);

    /// <inheritdoc />
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Value result)
    {
        if (ushort.TryParse(s, provider, out ushort value) && value <= MAX_REGISTER)
        {
            result = value;
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Value result)
    {
        if (ushort.TryParse(s, provider, out ushort value) && value <= MAX_REGISTER)
        {
            result = value;
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out Value result)
    {
        if (ushort.TryParse(s, style, provider, out ushort number) && number < MAX_REGISTER)
        {
            result = number;
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out Value result)
    {
        if (ushort.TryParse(s, style, provider, out ushort number) && number < MAX_REGISTER)
        {
            result = number;
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc cref="ushort.Min" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value Min(Value x, Value y) => Math.Min(x.value, y.value);

    /// <inheritdoc cref="ushort.Max" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value Max(Value x, Value y) => Math.Max(x.value, y.value);

    /// <inheritdoc cref="ushort.Clamp" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value Clamp(Value value, Value min, Value max) => Math.Clamp(value.value, min.value, max.value);

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">If <paramref name="left"/> or <paramref name="right"/> is a register</exception>
    public static (Value Quotient, Value Remainder) DivRem(Value left, Value right)
    {
        left.ThrowIfRegister();
        right.ThrowIfRegister();

        (ushort quotient, ushort remainder) = Math.DivRem(left, right);
        return (quotient, remainder);
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">If <paramref name="value"/> is a register</exception>
    public static Value RotateLeft(Value value, int rotateAmount)
    {
        value.ThrowIfRegister();

        rotateAmount %= BIT_COUNT;
        return new Value(((value.value << rotateAmount) | (value.value >> (BIT_COUNT - rotateAmount))) & MASK);
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">If <paramref name="value"/> is a register</exception>
    public static Value RotateRight(Value value, int rotateAmount)
    {
        value.ThrowIfRegister();

        rotateAmount %= BIT_COUNT;
        return new Value(((value.value >> rotateAmount) | (value.value << (BIT_COUNT - rotateAmount))) & MASK);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Sign(Value value) => value.value == 0 ? 0 : 1;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEvenInteger(Value value) => value.IsNumber && ushort.IsEvenInteger(value.value);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOddInteger(Value value) => value.IsNumber && ushort.IsOddInteger(value.value);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPow2(Value value) => value.IsNumber && ushort.IsPow2(value.value);

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">If <paramref name="value"/> is a register</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value Log2(Value value)
    {
        value.ThrowIfRegister();

        return ushort.Log2(value.value);
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">If <paramref name="value"/> is a register</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value PopCount(Value value)
    {
        value.ThrowIfRegister();

        return ushort.PopCount(value.value);
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">If <paramref name="value"/> is a register</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value LeadingZeroCount(Value value)
    {
        value.ThrowIfRegister();

        return ushort.LeadingZeroCount(value.value);
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">If <paramref name="value"/> is a register</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value TrailingZeroCount(Value value)
    {
        value.ThrowIfRegister();

        return ushort.TrailingZeroCount(value.value);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">If the resulting value is greater than <see cref="MAX_REGISTER"/></exception>
    public static Value CreateChecked<TOther>(TOther value) where TOther : INumberBase<TOther>
    {
        ushort result = ushort.CreateChecked(value);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(result, MAX_REGISTER, nameof(value));

        return result;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">If the resulting value is greater than <see cref="MAX_REGISTER"/></exception>
    public static Value CreateSaturating<TOther>(TOther value) where TOther : INumberBase<TOther>
    {
        ushort result = ushort.CreateSaturating(value);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(result, MAX_REGISTER, nameof(value));

        return result;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">If the resulting value is greater than <see cref="MAX_REGISTER"/></exception>
    public static Value CreateTruncating<TOther>(TOther value) where TOther : INumberBase<TOther>
    {
        ushort result = ushort.CreateTruncating(value);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(result, MAX_REGISTER, nameof(value));

        return result;
    }


    // === Casting Operators ===


    /// <summary>
    /// Implicit conversion from <see cref="Value"/> to <see cref="ushort"/>
    /// </summary>
    /// <param name="value"><see cref="Value"/> to convert to <see cref="ushort"/></param>
    /// <returns>The <see cref="ushort"/> value contained within this <see cref="Value"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ushort(Value value) => value.value;

    /// <summary>
    /// Implicit conversion from <see cref="ushort"/> to <see cref="Value"/>
    /// </summary>
    /// <param name="value"><see cref="ushort"/> to convert to <see cref="Value"/></param>
    /// <returns>The <see cref="Value"/> value representing this <see cref="ushort"/></returns>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="value"/> is greater than <see cref="MAX_REGISTER"/></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Value(ushort value) => new(value);

    /// <summary>
    /// Implicit conversion from <see cref="Value"/> to <see cref="Opcode"/>
    /// </summary>
    /// <param name="value"><see cref="Value"/> to convert to <see cref="Opcode"/></param>
    /// <returns>The <see cref="Opcode"/> value contained within this <see cref="Value"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Opcode(Value value) => (Opcode)value.value;

    /// <summary>
    /// Implicit conversion from <see cref="Opcode"/> to <see cref="Value"/>
    /// </summary>
    /// <param name="opcode"><see cref="Opcode"/> to convert to <see cref="Value"/></param>
    /// <returns>The <see cref="Value"/> value representing this <see cref="Opcode"/></returns>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="opcode"/> is greater than <see cref="MAX_REGISTER"/></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Value(Opcode opcode) => new((ushort)opcode);


    // === Mathematical Operators ===


    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">If <paramref name="value"/> is a register</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value operator +(Value value)
    {
        value.ThrowIfRegister();

        return value;
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">If <paramref name="value"/> is a register</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value operator -(Value value)
    {
        value.ThrowIfRegister();

        return new Value(-value.value & MASK);
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">If <paramref name="value"/> is a register</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value operator ++(Value value)
    {
        value.ThrowIfRegister();

        return  new Value((value.value + 1) & MASK);
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">If <paramref name="value"/> is a register</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value operator --(Value value)
    {
        value.ThrowIfRegister();

        return  new Value((value.value - 1) & MASK);
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">If either <paramref name="left"/> or <paramref name="right"/> are registers</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value operator +(Value left, Value right)
    {
        left.ThrowIfRegister();
        right.ThrowIfRegister();

        return  new Value((left.value + right.value) & MASK);
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">If either <paramref name="left"/> or <paramref name="right"/> are registers</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value operator -(Value left, Value right)
    {
        left.ThrowIfRegister();
        right.ThrowIfRegister();

        return  new Value((left.value - right.value) & MASK);
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">If either <paramref name="left"/> or <paramref name="right"/> are registers</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value operator *(Value left, Value right)
    {
        left.ThrowIfRegister();
        right.ThrowIfRegister();

        return  new Value((left.value * right.value) & MASK);
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">If either <paramref name="left"/> or <paramref name="right"/> are registers</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value operator /(Value left, Value right)
    {
        left.ThrowIfRegister();
        right.ThrowIfRegister();

        return  new Value(left.value / right.value);
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">If either <paramref name="left"/> or <paramref name="right"/> are registers</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value operator %(Value left, Value right)
    {
        left.ThrowIfRegister();
        right.ThrowIfRegister();

        return  new Value(left.value % right.value);
    }


    // === Bitwise Operators ===


    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">If <paramref name="value"/> is a register</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value operator ~(Value value)
    {
        value.ThrowIfRegister();

        return  new Value(~value.value & MASK);
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">If either <paramref name="left"/> or <paramref name="right"/> are registers</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value operator &(Value left, Value right)
    {
        left.ThrowIfRegister();
        right.ThrowIfRegister();

        return  new Value(left.value & right.value);
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">If either <paramref name="left"/> or <paramref name="right"/> are registers</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value operator |(Value left, Value right)
    {
        left.ThrowIfRegister();
        right.ThrowIfRegister();

        return  new Value(left.value | right.value);
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">If either <paramref name="left"/> or <paramref name="right"/> are registers</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value operator ^(Value left, Value right)
    {
        left.ThrowIfRegister();
        right.ThrowIfRegister();

        return  new Value(left.value ^ right.value);
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">If <paramref name="value"/> is a register</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value operator <<(Value value, int shiftAmount)
    {
        value.ThrowIfRegister();

        return  new Value((value.value << shiftAmount) & MASK);
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">If <paramref name="value"/> is a register</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value operator >>(Value value, int shiftAmount)
    {
        value.ThrowIfRegister();

        return  new Value(value.value >> shiftAmount);
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">If <paramref name="value"/> is a register</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value operator >>>(Value value, int shiftAmount)
    {
        value.ThrowIfRegister();

        return  new Value(value.value >>> shiftAmount);
    }


    // === Relational Operators ===


    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Value left, Value right) => left.value == right.value;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Value left, Value right) => left.value != right.value;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(Value left, Value right) => left.value > right.value;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >=(Value left, Value right) => left.value >= right.value;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(Value left, Value right) => left.value < right.value;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <=(Value left, Value right) => left.value <= right.value;


    // === Explicit Interface Implementations ===


    /// <inheritdoc />
    static Value INumberBase<Value>.Zero { get; } = 0;

    /// <inheritdoc />
    static Value INumberBase<Value>.One { get; } = 1;

    /// <inheritdoc />
    static int INumberBase<Value>.Radix { get; } = 2;

    /// <inheritdoc />
    static Value IMinMaxValue<Value>.MinValue { get; } = 0;

    /// <inheritdoc />
    static Value IMinMaxValue<Value>.MaxValue { get; } = MAX_VALUE;

    /// <inheritdoc />
    static Value IAdditiveIdentity<Value, Value>.AdditiveIdentity { get; } = 0;

    /// <inheritdoc />
    static Value IMultiplicativeIdentity<Value, Value>.MultiplicativeIdentity { get; } = 1;

    /// <inheritdoc />
    int IBinaryInteger<Value>.GetByteCount() => sizeof(ushort);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int IBinaryInteger<Value>.GetShortestBitLength() => (sizeof(ushort) * 8) - ushort.LeadingZeroCount(this.value);

    /// <inheritdoc />
    bool IBinaryInteger<Value>.TryWriteBigEndian(Span<byte> destination, out int bytesWritten)
    {
        if (BinaryPrimitives.TryWriteUInt16BigEndian(destination, this.value))
        {
            bytesWritten = sizeof(ushort);
            return true;
        }

        bytesWritten = 0;
        return false;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">If <paramref name="destination"/> is too short to write to</exception>
    int IBinaryInteger<Value>.WriteBigEndian(byte[] destination) => BinaryPrimitives.TryWriteUInt16BigEndian(destination, this.value)
                                                                        ? sizeof(ushort)
                                                                        : throw new ArgumentException("Destination too short", nameof(destination));

    /// <inheritdoc />
    /// <exception cref="ArgumentException">If <paramref name="destination"/> is too short to write to</exception>
    int IBinaryInteger<Value>.WriteBigEndian(byte[] destination, int startIndex) => BinaryPrimitives.TryWriteUInt16BigEndian(destination.AsSpan(startIndex), this.value)
                                                                                        ? sizeof(ushort)
                                                                                        : throw new ArgumentException("Destination too short", nameof(destination));

    /// <inheritdoc />
    /// <exception cref="ArgumentException">If <paramref name="destination"/> is too short to write to</exception>
    int IBinaryInteger<Value>.WriteBigEndian(Span<byte> destination) => BinaryPrimitives.TryWriteUInt16BigEndian(destination, this.value)
                                                                            ? sizeof(ushort)
                                                                            : throw new ArgumentException("Destination too short", nameof(destination));

    /// <inheritdoc />
    bool IBinaryInteger<Value>.TryWriteLittleEndian(Span<byte> destination, out int bytesWritten)
    {
        if (BinaryPrimitives.TryWriteUInt16LittleEndian(destination, this.value))
        {
            bytesWritten = sizeof(ushort);
            return true;
        }

        bytesWritten = 0;
        return false;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">If <paramref name="destination"/> is too short to write to</exception>
    int IBinaryInteger<Value>.WriteLittleEndian(byte[] destination) => BinaryPrimitives.TryWriteUInt16LittleEndian(destination, this.value)
                                                                        ? sizeof(ushort)
                                                                        : throw new ArgumentException("Destination too short", nameof(destination));

    /// <inheritdoc />
    /// <exception cref="ArgumentException">If <paramref name="destination"/> is too short to write to</exception>
    int IBinaryInteger<Value>.WriteLittleEndian(byte[] destination, int startIndex) => BinaryPrimitives.TryWriteUInt16LittleEndian(destination.AsSpan(startIndex), this.value)
                                                                                           ? sizeof(ushort)
                                                                                           : throw new ArgumentException("Destination too short", nameof(destination));

    /// <inheritdoc />
    /// <exception cref="ArgumentException">If <paramref name="destination"/> is too short to write to</exception>
    int IBinaryInteger<Value>.WriteLittleEndian(Span<byte> destination) => BinaryPrimitives.TryWriteUInt16LittleEndian(destination, this.value)
                                                                               ? sizeof(ushort)
                                                                               : throw new ArgumentException("Destination too short", nameof(destination));

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool IConvertible.ToBoolean(IFormatProvider? provider) => Convert.ToBoolean(this.value);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    char IConvertible.ToChar(IFormatProvider? provider) => Convert.ToChar(this.value);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    sbyte IConvertible.ToSByte(IFormatProvider? provider) => Convert.ToSByte(this.value);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    byte IConvertible.ToByte(IFormatProvider? provider) => Convert.ToByte(this.value);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    short IConvertible.ToInt16(IFormatProvider? provider) => Convert.ToInt16(this.value);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    ushort IConvertible.ToUInt16(IFormatProvider? provider) => Convert.ToUInt16(this.value);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int IConvertible.ToInt32(IFormatProvider? provider) => Convert.ToInt32(this.value);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    uint IConvertible.ToUInt32(IFormatProvider? provider) => Convert.ToUInt32(this.value);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    long IConvertible.ToInt64(IFormatProvider? provider) => Convert.ToInt64(this.value);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    ulong IConvertible.ToUInt64(IFormatProvider? provider) => Convert.ToUInt64(this.value);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    float IConvertible.ToSingle(IFormatProvider? provider) => Convert.ToSingle(this.value);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    double IConvertible.ToDouble(IFormatProvider? provider) => Convert.ToDouble(this.value);

    /// <inheritdoc />
    decimal IConvertible.ToDecimal(IFormatProvider? provider) => Convert.ToDecimal(this.value);

    /// <inheritdoc />
    /// <exception cref="InvalidCastException">Always thrown by this method</exception>
    [DoesNotReturn, MethodImpl(MethodImplOptions.AggressiveInlining)]
    DateTime IConvertible.ToDateTime(IFormatProvider? provider) => throw new InvalidCastException("Cannot case Value to DateTime");

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    object IConvertible.ToType(Type conversionType, IFormatProvider? provider) => ((IConvertible)this.value).ToType(conversionType, provider);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Value INumberBase<Value>.Abs(Value value) => value;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<Value>.IsZero(Value value) => value.value is 0;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<Value>.IsPositive(Value value) => true;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<Value>.IsNegative(Value value) => false;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<Value>.IsInteger(Value value) => value.IsNumber;

    /// <inheritdoc />
    static bool INumberBase<Value>.IsRealNumber(Value value) => value.IsNumber;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<Value>.IsComplexNumber(Value value) => false;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<Value>.IsImaginaryNumber(Value value) => false;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<Value>.IsCanonical(Value value) => true;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<Value>.IsNormal(Value value) => value.value is not 0;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<Value>.IsSubnormal(Value value) => false;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<Value>.IsFinite(Value value) => true;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<Value>.IsInfinity(Value value) => false;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<Value>.IsPositiveInfinity(Value value) => false;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<Value>.IsNegativeInfinity(Value value) => false;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<Value>.IsNaN(Value value) => false;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Value INumberBase<Value>.MaxMagnitude(Value x, Value y) => Max(x, y);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Value INumberBase<Value>.MaxMagnitudeNumber(Value x, Value y) => Max(x, y);

    /// <inheritdoc />
    static Value INumberBase<Value>.MinMagnitude(Value x, Value y) => Min(x, y);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Value INumberBase<Value>.MinMagnitudeNumber(Value x, Value y) => Min(x, y);

    /// <inheritdoc />
    static bool INumberBase<Value>.TryConvertFromChecked<TOther>(TOther value, out Value result)
    {
        if (TryConvertFromChecked(value, out ushort number) && number <= MAX_REGISTER)
        {
            result = number;
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc cref="TryConvertFromChecked" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryConvertFromChecked<TOther, TValue>(TOther value, out TValue result)
        where TOther : INumberBase<TOther>
        where TValue : unmanaged, INumberBase<TValue>
    {
        return TValue.TryConvertFromChecked(value, out result);
    }

    /// <inheritdoc />
    static bool INumberBase<Value>.TryConvertFromSaturating<TOther>(TOther value, out Value result)
    {
        if (TryConvertFromSaturating(value, out ushort number) && number <= MAX_REGISTER)
        {
            result = number;
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc cref="TryConvertFromSaturating" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryConvertFromSaturating<TOther, TValue>(TOther value, out TValue result)
        where TOther : INumberBase<TOther>
        where TValue : unmanaged, INumberBase<TValue>
    {
        return TValue.TryConvertFromSaturating(value, out result);
    }

    /// <inheritdoc />
    static bool INumberBase<Value>.TryConvertFromTruncating<TOther>(TOther value, out Value result)
    {
        if (TryConvertFromTruncating(value, out ushort number) && number <= MAX_REGISTER)
        {
            result = number;
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc cref="TryConvertFromTruncating" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryConvertFromTruncating<TOther, TValue>(TOther value, out TValue result)
        where TOther : INumberBase<TOther>
        where TValue : unmanaged, INumberBase<TValue>
    {
        return TValue.TryConvertFromTruncating(value, out result);
    }

    /// <inheritdoc />
    static bool INumberBase<Value>.TryConvertToChecked<TOther>(Value value, [MaybeNullWhen(false)] out TOther result)
    {
        return TryConvertToChecked(value.value, out result);
    }

    /// <inheritdoc cref="TryConvertToChecked" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryConvertToChecked<TOther, TValue>(TValue value, [MaybeNullWhen(false)] out TOther result)
        where TOther : INumberBase<TOther>
        where TValue : unmanaged, INumberBase<TValue>
    {
        return TValue.TryConvertToChecked(value, out result);
    }

    /// <inheritdoc />
    static bool INumberBase<Value>.TryConvertToSaturating<TOther>(Value value, [MaybeNullWhen(false)] out TOther result)
    {
        return TryConvertToSaturating(value.value, out result);
    }

    /// <inheritdoc cref="TryConvertToSaturating" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryConvertToSaturating<TOther, TValue>(TValue value, [MaybeNullWhen(false)] out TOther result)
        where TOther : INumberBase<TOther>
        where TValue : unmanaged, INumberBase<TValue>
    {
        return TValue.TryConvertToSaturating(value, out result);
    }

    /// <inheritdoc />
    static bool INumberBase<Value>.TryConvertToTruncating<TOther>(Value value, [MaybeNullWhen(false)] out TOther result)
    {
        return TryConvertToTruncating(value.value, out result);
    }

    /// <inheritdoc cref="TryConvertToTruncating" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryConvertToTruncating<TOther, TValue>(TValue value, [MaybeNullWhen(false)] out TOther result)
        where TOther : INumberBase<TOther>
        where TValue : unmanaged, INumberBase<TValue>
    {
        return TValue.TryConvertToTruncating(value, out result);
    }

    /// <inheritdoc />
    static bool IBinaryInteger<Value>.TryReadBigEndian(ReadOnlySpan<byte> source, bool isUnsigned, out Value value)
    {
        if (TryReadBigEndian(source, isUnsigned, out ushort number) && number < MAX_REGISTER)
        {
            value = number;
            return true;
        }

        value = default;
        return false;
    }

    /// <inheritdoc cref="TryReadBigEndian" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryReadBigEndian<TOther>(ReadOnlySpan<byte> source, bool isUnsigned, out TOther value)
        where TOther : IBinaryInteger<TOther>
    {
        return TOther.TryReadBigEndian(source, isUnsigned, out value);
    }

    /// <inheritdoc />
    public static bool TryReadLittleEndian(ReadOnlySpan<byte> source, bool isUnsigned, out Value value)
    {
        if (TryReadLittleEndian(source, isUnsigned, out ushort number) && number < MAX_REGISTER)
        {
            value = number;
            return true;
        }

        value = default;
        return false;
    }

    /// <inheritdoc cref="TryReadLittleEndian" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryReadLittleEndian<TOther>(ReadOnlySpan<byte> source, bool isUnsigned, out TOther value)
        where TOther : IBinaryInteger<TOther>
    {
        return TOther.TryReadLittleEndian(source, isUnsigned, out value);
    }
}
