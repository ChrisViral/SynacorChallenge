# SynacorChallenge

Synacor Challenge implementation in C#.

## Overview

The repository contains a complete implementation of the Synacor challenge virtual machine (VM) in C# with .NET 10.0

* **Synacor** – a class library exposing `VirtualMachine`, `Opcode` and helpers.
* **Synacor.CLI** – a command‑line tool that bundles the VM and a small set of helper commands.
* **Synacor.Tests** – a comprehensive test suite for the VM, stack and 15‑bit integer logic.

The CLI ships as a .NET global tool (command name `synacor`).

## Building

```bash
# Restore dependencies
	dotnet restore

# Build (Release)
	dotnet build --configuration Release
```

The build produces a global tool package in `Synacor.CLI/bin/Release/net10.0/*.nupkg`.  To install the tool locally you can run:

```bash
# Install the tool from the local package source
	dotnet tool install --global --add-source ./Synacor.CLI/bin/Release/net10.0 Synacor.CLI
```

After installation the `synacor` command is available from any terminal.

### Sub‑commands

| Command | Description | Usage example |
|---------|-------------|---------------|
| `run` | Execute a Synacor binary.  Supports optional register and memory patches, and state load/save. | `synacor run challenge.bin --register-patches a:123 b:456 --memory-patches 42:LOAD` |
| `coins` | Solve the 5‑coin equation `c1 + c2*c3^2 + c4^3 - c5 = result`. | `synacor coins 1 2 3 4 5 100` |
| `teleport` | Find the eighth register value that satisfies the teleport check given initial A, B and expected result. | `synacor teleport 3 5 2` |
| `orb` | Find the shortest path through an orb graph file to reach a specified door value. | `synacor orb graph.txt 42` |

#### The `run` command details

```
synacor run <binary-file> [options]
```

* `<binary-file>` – Path to the 16‑bit little‑endian Synacor binary.
* `--from-state` – Treat the file as a previously‑dumped VM state rather than a binary.
* `--save-state` – When the VM is aborted (Ctrl‑C) save the current state to `<binary-file>.vmd`.
* `--register-patches a:123 b:456` – Patch register *a* to 123, register *b* to 456, etc. Register names are `a`–`h`.
* `--memory-patches 42:LOAD` – Patch the memory at address 42 with the `LOAD` opcode (the opcode name comes from the `Opcode` enum).

All patches are validated against the `[a-h]:\d{1,5}` and `\d{1,5}:[A-Za-z]{2,4}` patterns.

#### The `coins` command details

```
synacor coins <a> <b> <c> <d> <e> <result>
```

The command takes the five coin values followed by the expected result of the equation:

```
result = a + b * c^2 + d^3 - e
```

It searches all permutations of the coins until it finds an ordering that satisfies the equation.  On success it prints the solved order and exits with code 0; on failure it reports no solution and exits with code 1.

#### The `orb` command details

```
synacor orb <graph-file> <door-value>
```

The graph file is a space‑separated grid of integers, one row per line.  The tool reads the file, builds a graph and performs a breadth‑first search to find the shortest sequence of operations that reaches the given door value.

#### The `teleport` command details

```
synacor teleport <initial-a> <initial-b> <expected-result>
```

The teleport command implements the teleport check described in the Synacor challenge.  It receives the initial values for registers **A** and **B**, and the expected result after the teleport check.  The tool exhaustively searches for a register value **h** (the eighth register) that, when used in the teleport algorithm, yields the expected result.  If a valid value is found, it is printed and the command exits with code 0; otherwise it exits with code 1.

The algorithm uses a nested iteration over all possible register values (0–`Value.MAX_VALUE`) and applies the teleport calculation logic defined in `TeleportCommand`.  The search is deterministic and finishes quickly for the 15‑bit address space.
