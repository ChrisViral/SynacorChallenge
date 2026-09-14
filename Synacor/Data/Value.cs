using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using JetBrains.Annotations;

namespace Synacor.Data;

/// <summary>
/// Numerical or register value
/// </summary>
[PublicAPI]
public readonly struct Value : IBinaryInteger<Value>, IUnsignedNumber<Value>, IMinMaxValue<Value>, IConvertible
{
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
    /// Mathematical operation modulo value
    /// </summary>
    private const ushort MODULO = MAX_VALUE + 1;

    /// <summary>
    /// Contained numerical value
    /// </summary>
    private readonly ushort value;

    /// <summary>
    /// If this <see cref="Value"/> contains a number
    /// </summary>
    public bool IsNumber => this.value <= MAX_VALUE;

    /// <summary>
    /// If this <see cref="Value"/> contains a register address
    /// </summary>
    public bool IsRegister => this.value is > MAX_VALUE and <= MAX_REGISTER;

    /// <summary>
    /// Register address character for this <see cref="Value"/>
    /// </summary>
    private char RegisterChar => (char)(this.value + ('a' - MAX_VALUE));

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

    /// <inheritdoc />
    public bool Equals(Value other) => this.value == other.value;

    /// <inheritdoc />
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is Value other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => this.value;

    /// <inheritdoc />
    public int CompareTo(Value other) => this.value.CompareTo(other.value);

    /// <inheritdoc />
    public int CompareTo(object? obj) => this.value.CompareTo(obj);

    /// <summary>
    /// Converts this <see cref="Value"/> to either it's numberical string or register address string
    /// </summary>
    /// <returns>The string representation of this <see cref="Value"/></returns>
    public override string ToString() => this.IsNumber
                                             ? this.value.ToString()
                                             : this.RegisterChar.ToString();

    /// <inheritdoc />
    public string ToString(IFormatProvider? provider) => this.IsNumber
                                                             ? this.value.ToString(provider)
                                                             : this.RegisterChar.ToString(provider);

    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider? formatProvider) => this.IsNumber
                                                                                   ? this.value.ToString(format, formatProvider)
                                                                                   : this.RegisterChar.ToString(formatProvider);

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
    public TypeCode GetTypeCode() => this.value.GetTypeCode();

    // === Static Methods ===

    /// <inheritdoc />
    public static Value Parse(string s, IFormatProvider? provider) => ushort.Parse(s, provider);

    /// <inheritdoc />
    public static Value Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => ushort.Parse(s, provider);

    /// <inheritdoc />
    public static Value Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) => ushort.Parse(s, style, provider);

    /// <inheritdoc />
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
    public static Value Min(Value x, Value y) => Math.Min(x.value, y.value);

    /// <inheritdoc cref="ushort.Max" />
    public static Value Max(Value x, Value y) => Math.Max(x.value, y.value);

    /// <inheritdoc cref="ushort.Clamp" />
    public static Value Clamp(Value value, Value min, Value max) => Math.Clamp(value.value, min.value, max.value);

    /// <inheritdoc />
    public static (Value Quotient, Value Remainder) DivRem(Value left, Value right)
    {
        (ushort quotient, ushort remainder) = Math.DivRem(left, right);
        return (quotient, remainder);
    }

    /// <inheritdoc />
    public static Value RotateLeft(Value value, int rotateAmount) => ushort.RotateLeft(value.value, rotateAmount);

    /// <inheritdoc />
    public static Value RotateRight(Value value, int rotateAmount) => ushort.RotateRight(value.value, rotateAmount);

    /// <inheritdoc />
    public static int Sign(Value value) => value.value == 0 ? 0 : 1;

    /// <inheritdoc />
    public static bool IsEvenInteger(Value value) => ushort.IsEvenInteger(value.value);

    /// <inheritdoc />
    public static bool IsOddInteger(Value value) => ushort.IsOddInteger(value);

    /// <inheritdoc />
    public static bool IsPow2(Value value) => ushort.IsPow2(value.value);

    /// <inheritdoc />
    public static Value Log2(Value value) => ushort.Log2(value.value);

    /// <inheritdoc />
    public static Value PopCount(Value value) => ushort.PopCount(value.value);

    /// <inheritdoc />
    public static Value LeadingZeroCount(Value value) => ushort.LeadingZeroCount(value.value);

    /// <inheritdoc />
    public static Value TrailingZeroCount(Value value) => ushort.TrailingZeroCount(value.value);

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
    public static implicit operator ushort(Value value) => value.value;

    /// <summary>
    /// Implicit conversion from <see cref="ushort"/> to <see cref="Value"/>
    /// </summary>
    /// <param name="value"><see cref="ushort"/> to convert to <see cref="Value"/></param>
    /// <returns>The <see cref="Value"/> value representing this <see cref="ushort"/></returns>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="value"/> is greater than <see cref="MAX_REGISTER"/></exception>
    public static implicit operator Value(ushort value) => new(value);

    /// <summary>
    /// Implicit conversion from <see cref="int"/> to <see cref="Value"/>
    /// </summary>
    /// <param name="value"><see cref="int"/> to convert to <see cref="Value"/></param>
    /// <returns>The <see cref="Value"/> value representing this <see cref="int"/></returns>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="value"/> is greater than <see cref="MAX_REGISTER"/></exception>
    public static implicit operator Value(int value) => new((ushort)value);

    // === Operators ===

    /// <inheritdoc />
    public static Value operator +(Value value) => value;

    /// <inheritdoc />
    public static Value operator -(Value value) => (MAX_VALUE + 1 - value.value) % MODULO;

    /// <inheritdoc />
    public static Value operator ++(Value value) => (value + 1) % MODULO;

    /// <inheritdoc />
    public static Value operator --(Value value) => (value - 1) % MODULO;

    /// <inheritdoc />
    public static Value operator +(Value left, Value right) => (left.value + right.value) % MODULO;

    /// <inheritdoc />
    public static Value operator -(Value left, Value right) => (left.value - right.value) % MODULO;

    /// <inheritdoc />
    public static Value operator *(Value left, Value right) => (left.value * right.value) % MODULO;

    /// <inheritdoc />
    public static Value operator /(Value left, Value right) => (left.value / right.value) % MODULO;

    /// <inheritdoc />
    public static Value operator %(Value left, Value right) => left.value % right.value;

    /// <inheritdoc />
    public static Value operator ~(Value value) => ~value.value % MODULO;

    /// <inheritdoc />
    public static Value operator &(Value left, Value right) => (left.value & right.value) % MODULO;

    /// <inheritdoc />
    public static Value operator |(Value left, Value right) => (left.value | right.value) % MODULO;

    /// <inheritdoc />
    public static Value operator ^(Value left, Value right) => (left.value ^ right.value) % MODULO;

    /// <inheritdoc />
    public static Value operator <<(Value value, int shiftAmount) => (value.value << shiftAmount) % MODULO;

    /// <inheritdoc />
    public static Value operator >> (Value value, int shiftAmount) => (value.value >> shiftAmount) % MODULO;

    /// <inheritdoc />
    public static Value operator >>> (Value value, int shiftAmount) => (value.value >>> shiftAmount) % MODULO;

    /// <inheritdoc />
    public static bool operator ==(Value left, Value right) => left.value == right.value;

    /// <inheritdoc />
    public static bool operator !=(Value left, Value right) => left.value != right.value;

    /// <inheritdoc />
    public static bool operator >(Value left, Value right) => left.value > right.value;

    /// <inheritdoc />
    public static bool operator >=(Value left, Value right) => left.value >= right.value;

    /// <inheritdoc />
    public static bool operator <(Value left, Value right) => left.value < right.value;

    /// <inheritdoc />
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
    int IBinaryInteger<Value>.WriteBigEndian(byte[] destination) => BinaryPrimitives.TryWriteUInt16BigEndian(destination, this.value)
                                                                        ? sizeof(ushort)
                                                                        : throw new ArgumentException("Destination too short", nameof(destination));

    /// <inheritdoc />
    int IBinaryInteger<Value>.WriteBigEndian(byte[] destination, int startIndex) => BinaryPrimitives.TryWriteUInt16BigEndian(destination.AsSpan(startIndex), this.value)
                                                                                        ? sizeof(ushort)
                                                                                        : throw new ArgumentException("Destination too short", nameof(destination));

    /// <inheritdoc />
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
    int IBinaryInteger<Value>.WriteLittleEndian(byte[] destination) => BinaryPrimitives.TryWriteUInt16LittleEndian(destination, this.value)
                                                                        ? sizeof(ushort)
                                                                        : throw new ArgumentException("Destination too short", nameof(destination));

    /// <inheritdoc />
    int IBinaryInteger<Value>.WriteLittleEndian(byte[] destination, int startIndex) => BinaryPrimitives.TryWriteUInt16LittleEndian(destination.AsSpan(startIndex), this.value)
                                                                                           ? sizeof(ushort)
                                                                                           : throw new ArgumentException("Destination too short", nameof(destination));

    /// <inheritdoc />
    int IBinaryInteger<Value>.WriteLittleEndian(Span<byte> destination) => BinaryPrimitives.TryWriteUInt16LittleEndian(destination, this.value)
                                                                               ? sizeof(ushort)
                                                                               : throw new ArgumentException("Destination too short", nameof(destination));

    /// <inheritdoc />
    bool IConvertible.ToBoolean(IFormatProvider? provider) => Convert.ToBoolean(this.value);

    /// <inheritdoc />
    char IConvertible.ToChar(IFormatProvider? provider) => Convert.ToChar(this.value);

    /// <inheritdoc />
    sbyte IConvertible.ToSByte(IFormatProvider? provider) => Convert.ToSByte(this.value);

    /// <inheritdoc />
    byte IConvertible.ToByte(IFormatProvider? provider) => Convert.ToByte(this.value);

    /// <inheritdoc />
    short IConvertible.ToInt16(IFormatProvider? provider) => Convert.ToInt16(this.value);

    /// <inheritdoc />
    ushort IConvertible.ToUInt16(IFormatProvider? provider) => Convert.ToUInt16(this.value);

    /// <inheritdoc />
    int IConvertible.ToInt32(IFormatProvider? provider) => Convert.ToInt32(this.value);

    /// <inheritdoc />
    uint IConvertible.ToUInt32(IFormatProvider? provider) => Convert.ToUInt32(this.value);

    /// <inheritdoc />
    long IConvertible.ToInt64(IFormatProvider? provider) => Convert.ToInt64(this.value);

    /// <inheritdoc />
    ulong IConvertible.ToUInt64(IFormatProvider? provider) => Convert.ToUInt64(this.value);

    /// <inheritdoc />
    float IConvertible.ToSingle(IFormatProvider? provider) => Convert.ToSingle(this.value);

    /// <inheritdoc />
    double IConvertible.ToDouble(IFormatProvider? provider) => Convert.ToDouble(this.value);

    /// <inheritdoc />
    decimal IConvertible.ToDecimal(IFormatProvider? provider) => Convert.ToDecimal(this.value);

    /// <inheritdoc />
    DateTime IConvertible.ToDateTime(IFormatProvider? provider) => throw new InvalidCastException("Cannot case UInt16 to DateTime");

    /// <inheritdoc />
    object IConvertible.ToType(Type conversionType, IFormatProvider? provider) => ((IConvertible)this.value).ToType(conversionType, provider);

    /// <inheritdoc />
    static Value INumberBase<Value>.Abs(Value value) => value;

    /// <inheritdoc />
    static bool INumberBase<Value>.IsZero(Value value) => value.value == 0;

    /// <inheritdoc />
    static bool INumberBase<Value>.IsPositive(Value value) => true;

    /// <inheritdoc />
    static bool INumberBase<Value>.IsNegative(Value value) => false;

    /// <inheritdoc />
    static bool INumberBase<Value>.IsInteger(Value value) => true;

    /// <inheritdoc />
    static bool INumberBase<Value>.IsRealNumber(Value value) => true;

    /// <inheritdoc />
    static bool INumberBase<Value>.IsComplexNumber(Value value) => false;

    /// <inheritdoc />
    static bool INumberBase<Value>.IsImaginaryNumber(Value value) => false;

    /// <inheritdoc />
    static bool INumberBase<Value>.IsCanonical(Value value) => true;

    /// <inheritdoc />
    static bool INumberBase<Value>.IsNormal(Value value) => value.value != 0;

    /// <inheritdoc />
    static bool INumberBase<Value>.IsSubnormal(Value value) => false;

    /// <inheritdoc />
    static bool INumberBase<Value>.IsFinite(Value value) => true;

    /// <inheritdoc />
    static bool INumberBase<Value>.IsInfinity(Value value) => false;

    /// <inheritdoc />
    static bool INumberBase<Value>.IsPositiveInfinity(Value value) => false;

    /// <inheritdoc />
    static bool INumberBase<Value>.IsNegativeInfinity(Value value) => false;

    /// <inheritdoc />
    static bool INumberBase<Value>.IsNaN(Value value) => false;

    /// <inheritdoc />
    static Value INumberBase<Value>.MaxMagnitude(Value x, Value y) => Max(x, y);

    /// <inheritdoc />
    static Value INumberBase<Value>.MaxMagnitudeNumber(Value x, Value y) => Max(x, y);

    /// <inheritdoc />
    static Value INumberBase<Value>.MinMagnitude(Value x, Value y) => Min(x, y);

    /// <inheritdoc />
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
    private static bool TryReadBigEndian<T>(ReadOnlySpan<byte> source, bool isUnsigned, out T value)
        where T : IBinaryInteger<T>
    {
        return T.TryReadBigEndian(source, isUnsigned, out value);
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
    private static bool TryReadLittleEndian<T>(ReadOnlySpan<byte> source, bool isUnsigned, out T value)
        where T : IBinaryInteger<T>
    {
        return T.TryReadLittleEndian(source, isUnsigned, out value);
    }
}
