# Csproj Checker

A WinForms application for checking and managing .csproj files.

## Requirements

- .NET 9.0 SDK or later
- Windows OS (for running the application)

## Building

```bash
dotnet build
```

## Running

```bash
dotnet run --project CsprojChecker
```

## Features

### Current (Step 1 - UI Skeleton)
- Main window with folder selection
- DataGridView for displaying project information
- Framework operations region (placeholder)
- Project style conversions region (placeholder)
- Event handlers wired up (no business logic yet)

### Planned
- Browse for folders containing .csproj files
- Scan and analyze project files
- Display project information (path, style, target frameworks)
- Framework operations functionality
- Project style conversion functionality

## Project Structure

- `CsprojChecker/` - Main WinForms application
  - `Program.cs` - Application entry point
  - `MainForm.cs` - Main application form with UI layout
