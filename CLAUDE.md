# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is WoofWare.BalancedReducer, a F# port of Jane Street's balanced reducer from core_kernel. It provides a data structure that incrementally maintains the result of folding an associative operation over a mutable fixed-length sequence as its elements change.

## Architecture

The project consists of two main components:
- `WoofWare.BalancedReducer/` - The main library targeting netstandard2.0
- `WoofWare.BalancedReducer.Test/` - NUnit tests targeting net9.0

### Core Implementation
- `BalancedReducer.fsi` - Public API with comprehensive documentation
- `BalancedReducer.fs` - Implementation using an implicit binary tree structure stored in an array

The balanced reducer uses a clever binary tree representation where:
- Internal nodes store partial reductions
- Leaves contain the actual values
- The tree is complete but not necessarily perfect
- Leaf rotation ensures proper ordering preservation

## Development Commands

### Building and Testing
```bash
# Restore dependencies
nix develop --command dotnet restore

# Build the solution
nix develop --command dotnet build --no-restore --configuration Release

# Run tests
nix develop --command dotnet test

# Pack for NuGet
nix develop --command dotnet pack --configuration Release
```

### Code Formatting and Analysis
```bash
# Format F# code with Fantomas
nix run .#fantomas -- .

# Check formatting without applying changes
nix run .#fantomas -- --check .

# Run F# analyzers
dotnet fsharp-analyzers

# Format Nix files
nix develop --command alejandra .
```

### Git Operations
- Use `git diff --no-ext-diff` when running diff commands due to external diff tool configuration

### Build Environment
The project uses Nix for reproducible builds and development environments. All commands should be run within the Nix development shell using `nix develop --command <command>`.

## Testing Strategy
- Tests use NUnit framework with FsUnit and WoofWare.Expect
- API surface testing with ApiSurface package
- Tests verify both functionality and invariant preservation
- Tests include logging of reduction operations for debugging

## Project Configuration
- Uses Nerdbank.GitVersioning for automatic versioning
- Treats warnings as errors across the codebase
- Configured for deterministic builds
- SourceLink integration for debugging
- API surface baseline tracking with `SurfaceBaseline.txt`