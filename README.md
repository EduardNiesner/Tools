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

### Current (Step 3 - Variable TFM token support and selection enablement)
- Main window with folder selection
- Browse button to select folders containing .csproj files
- Recursive async scanning for .csproj files
- Real-time DataGridView updates as files are discovered
- Parse and display project style (SDK vs Old-style)
- Parse and display target framework(s) with variable token support
- Status label showing scan progress
- Cancel button to stop scans in progress
- No UI freezing during large scans
- **Framework Operations:**
  - Configurable Variable TFM token TextBox (default: `$(TargetFrameworks)`)
  - Display inherited TFMs using the configured token
  - "Change target framework" button (enabled when all selected rows share the same normalized TFM set)
  - Target framework ComboBox (prefilled with common TFM set when enabled)
  - TFM normalization: order-insensitive, case-insensitive for literals, exact match for variables
- **Responsive GUI:** Full screen support with proper anchoring for all controls
- Project style conversions region (placeholder)

### Planned
- Framework operations functionality
- Project style conversion functionality

## Project Structure

- `CsprojChecker/` - Main WinForms application
  - `Program.cs` - Application entry point
  - `MainForm.cs` - Main application form with UI layout and scanning logic

## Implementation Details

### Async Folder Scanning
The application uses async/await patterns to scan folders recursively without blocking the UI thread:
- Recursively scans all subdirectories (no folder filtering)
- Updates the DataGridView incrementally as files are found
- Supports cancellation via CancellationToken
- Handles unauthorized access gracefully

### .csproj Parsing
Uses XML parsing to determine project characteristics:
- **SDK-style detection**: Checks for `Sdk` attribute on root element
- **Target Framework parsing**: 
  - Reads `TargetFramework` (single)
  - Reads `TargetFrameworks` (multiple, semicolon-separated)
  - Reads `TargetFrameworkVersion` (old-style projects)
  - Preserves variable tokens like `$(TargetFramework)` for inherited values

### UI Features
- **Browse Button**: Opens folder browser dialog with memory of last path
- **Check Button**: Initiates async scan with real-time updates
- **Cancel Button**: Stops in-progress scans
- **Status Label**: Shows progress during scans
- **Button Heights**: Increased by 25% for better visibility
- **Responsive Layout**: All controls properly anchored for full-screen usage
- **DataGridView**: Expands to fill available space for viewing long paths

### Framework Operations
- **Variable TFM Token TextBox**: Configure the variable token used for inherited TFMs (default: `$(TargetFrameworks)`)
- **Selection-based Enablement**: The "Change target framework" button and ComboBox are only enabled when:
  - One or more rows are selected
  - All selected rows have the same normalized TFM set
- **TFM Normalization Rules**:
  - **Order-insensitive**: `net6.0;net7.0;net8.0` equals `net8.0;net7.0;net6.0`
  - **Case-insensitive for literals**: `NET6.0;NET7.0` equals `net6.0;net7.0`
  - **Exact match for variables**: `$(TargetFrameworks)` only matches `$(TargetFrameworks)` exactly
  - **Variable vs Literal**: Variable tokens and literal TFMs are never equal
- **ComboBox Prefill**: When enabled, the ComboBox is automatically populated with the common TFM set from selected rows
