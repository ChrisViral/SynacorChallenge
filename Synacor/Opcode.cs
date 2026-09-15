namespace Synacor;

/// <summary>
/// <see cref="VirtualMachine"/> Opcodes
/// </summary>
public enum Opcode : ushort
{
    /// <summary> Stop execution and terminate the program </summary>
    HALT = 0,
    /// <summary> Set register <c>a</c> to the value of register <c>b</c> </summary>
    SET  = 1,
    /// <summary> Push <c>a</c> onto the stack </summary>
    PUSH = 2,
    /// <summary> Remove the top element from the stack and write it into <c>a</c>; empty stack = error </summary>
    POP  = 3,
    /// <summary> Set <c>a</c> to 1 if <c>b</c> is equal to <c>c</c>; set it to 0 otherwise </summary>
    EQ   = 4,
    /// <summary> Set <c>a</c> to 1 if <c>b</c> is greater than <c>c</c>; set it to 0 otherwise </summary>
    GT   = 5,
    /// <summary> Jump to <c>a</c> </summary>
    JMP  = 6,
    /// <summary> If <c>a</c> is nonzero, jump to <c>b</c> </summary>
    JT   = 7,
    /// <summary> If <c>a</c> is zero, jump to <c>b</c> </summary>
    JF   = 8,
    /// <summary> Assign into <c>a</c> the sum of <c>b</c> and <c>c</c> (modulo 32768) </summary>
    ADD  = 9,
    /// <summary> Store into <c>a</c> the product of <c>b</c> and <c>c</c> (modulo 32768) </summary>
    MULT = 10,
    /// <summary> Store into <c>a</c> the remainder of <c>b</c> divided by <c>c</c> </summary>
    MOD  = 11,
    /// <summary> Stores into <c>a</c> the bitwise and of <c>b</c> and <c>c</c> </summary>
    AND  = 12,
    /// <summary> Stores into <c>a</c> the bitwise or of <c>b</c> and <c>c</c> </summary>
    OR   = 13,
    /// <summary> Stores 15-bit bitwise inverse of <c>b</c> in <c>a</c> </summary>
    NOT  = 14,
    /// <summary> Read memory at address <c>b</c> and write it to <c>a</c> </summary>
    RMEM = 15,
    /// <summary> Write the value from <c>b</c> into memory at address <c>a</c> </summary>
    WMEM = 16,
    /// <summary> Write the address of the next instruction to the stack and jump to <c>a</c> </summary>
    CALL = 17,
    /// <summary> Remove the top element from the stack and jump to it; empty stack = halt </summary>
    RET  = 18,
    /// <summary> Write the character represented by ascii code <c>a</c> to the terminal </summary>
    OUT  = 19,
    /// <summary>
    /// Read a character from the terminal and write its ascii code to <c>a</c>.
    /// It can be assumed that once input starts, it will continue until a newline is encountered.
    /// This means that you can safely read whole lines from the keyboard instead of having to figure out how to read individual characters.
    /// </summary>
    IN   = 20,
    /// <summary> No operation </summary>
    NOOP = 21
}
