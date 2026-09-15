using System.Runtime.CompilerServices;
using Synacor.Data;

namespace Synacor;

public partial class VirtualMachine
{
    /// <summary>
    /// Gets the current <see cref="Opcode"/> and increments the instruction pointer
    /// </summary>
    /// <returns>The <see cref="Opcode"/> for the current instruction</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe Opcode GetOpcode() => *this.ip++;

    /// <summary>
    /// Increments the instruction pointer
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void MoveNext() => this.ip++;

    /// <summary>
    /// Gets the <see cref="Value"/> at a current instruction pointer, dereferencing registers if needed,
    /// and increments the instruction pointer
    /// </summary>
    /// <returns>The <see cref="Value"/> numerical value or register value for the current instruction</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe Value GetValue()
    {
        Value value = *this.ip++;
        return value.IsRegister
                       ? *(this.memory + value)
                       : value;
    }

    /// <summary>
    /// Gets a reference to the register pointed to by the current instruction pointer,
    /// and increments the instruction pointer
    /// </summary>
    /// <returns>A reference to the register pointed to by the current instruction</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe ref Value GetRegister()
    {
        Value register = *this.ip++;
#if DEBUG
        register.ThrowIfNumber();
#endif
        return ref *(this.memory + register);
    }

    /// <summary>
    /// Gets a reference to the <see cref="Value"/> in the memory address of the current instruction pointer,
    /// dereferencing registers if necessary, and then increments the instruction pointer
    /// </summary>
    /// <returns>A reference to the memory pointed to by the current instruction</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe ref Value GetMemory()
    {
        Value offset = *this.ip++;
        return ref offset.IsRegister
                   ? ref *(this.memory + *(this.memory + offset))
                   : ref *(this.memory + offset);
    }

    /// <summary>
    /// Jumps to the instruction pointed at by the current instruction pointer, dereferening registers if necessary
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void Jump()
    {
        Value value = *this.ip;
        this.ip = this.memory
                + (value.IsRegister
                       ? *(this.memory + value)
                       : value);
    }

    /// <summary>
    /// Jumps to the instruction at the given address
    /// </summary>
    /// <param name="adress">Adress to jump to</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void Jump(Value adress) => this.ip = this.memory + adress;

    /// <summary>
    /// Gets the numerical address at the given offset from the current instruction pointer
    /// </summary>
    /// <returns>The memory address offset of the instruction pointer</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe Value GetAddress(int offset) => (ushort)((this.ip + offset) - this.memory);
}
