# == Synacor OSCON 2012 Challenge ==
In this challenge, your job is to use this architecture spec to create a
virtual machine capable of running the included binary.  
Along the way, you will find codes; submit these to the challenge website to track
your progress.

Good luck!

---

## == Architecture ==
- Three storage regions
  - Memory with 15-bit address space storing 16-bit values
  - Eight registers
  - An unbounded stack which holds individual 16-bit values
- All numbers are unsigned integers 0..32767 (15-bit)
- All math is modulo 32768; 32758 + 15 => 5

## == Binary Format ==
- Each number is stored as a 16-bit little-endian pair (low byte, high byte)
- Numbers 0..32767 mean a literal value
- Numbers 32768..32775 instead mean registers 0..7
- Numbers 32776..65535 are invalid
- Programs are loaded into memory starting at address 0
- Address 0 is the first 16-bit value, address 1 is the second 16-bit value, etc.

## == Execution ==
- After an operation is executed, the next instruction to read is immediately after the last argument of the current operation.
  If a jump was performed, the next operation is instead the exact destination of the jump.
- Encountering a register as an operation argument should be taken as reading from the register or setting into the register as appropriate.

## == Hints ==
- Start with operations 0, 19, and 21.
- Here's a code for the challenge website: LDOb7UGhTi
- The program "9,32768,32769,4,19,32768" occupies six memory addresses and should:
  - Store into register 0 the sum of 4 and the value contained in register 1.
  - Output to the terminal the character with the ascii code contained in register 0.

## == Opcodes ==
- halt: 0
  - Stop execution and terminate the program
- set: 1 a b
    - Set register `a` to the value of `b`
- push: 2 a
    - Push `a` onto the stack
- pop: 3 a
    - Remove the top element from the stack and write it into `a`; empty stack = error
- eq: 4 a b c
    - Set `a` to 1 if `b` is equal to `c`; set it to 0 otherwise
- gt: 5 a b c
    - Set `a` to 1 if `b` is greater than `c`; set it to 0 otherwise
- jmp: 6 a
  - Jump to `a`
- jt: 7 a b
  - If `a` is nonzero, jump to `b`
- jf: 8 a b
  - If `a` is zero, jump to `b`
- add: 9 a b c
  - Assign into `a` the sum of `b` and `c` (modulo 32768)
- mult: 10 a b c
  - Store into `a` the product of `b` and `c` (modulo 32768)
- mod: 11 a b c
  - Store into `a` the remainder of `b` divided by `c`
- and: 12 a b c
  - Stores into `a` the bitwise and of `b` and `c`
- or: 13 a b c
  - Stores into `a` the bitwise or of `b` and `c`
- not: 14 a b
  - Stores 15-bit bitwise inverse of `b` in `a`
- rmem: 15 a b
  - Read memory at address `b` and write it to `a`
- wmem: 16 a b
  - Write the value from `b` into memory at address `a`
- call: 17 a
  - Write the address of the next instruction to the stack and jump to `a`
- ret: 18
  - Remove the top element from the stack and jump to it; empty stack = halt
- out: 19 a
  - Write the character represented by ascii code `a` to the terminal
- in: 20 a
  - Read a character from the terminal and write its ascii code to `a`.
    It can be assumed that once input starts, it will continue until a newline is encountered.
    This means that you can safely read whole lines from the keyboard instead of having to figure out how to read individual characters.
- noop: 21
  - No operation
