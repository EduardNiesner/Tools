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

### Current (Step 5 - Append framework for SDK-only projects)
- Main window with folder selection
- Browse button to select folders containing .csproj files
- Recursive async scanning for .csproj files
- Real-time DataGridView updates as files are discovered
- Parse and display project style (SDK vs Old-style)
- Parse and display target framework(s) exactly as they appear in csproj (including variables like $(TargetFrameworks))
- Status label showing scan progress
- Cancel button to stop scans in progress
- No UI freezing during large scans
- **Framework Operations:**
  - "Change target framework" button (enabled only when all selected rows have identical TFM sets)
  - Target framework ComboBox (prefilled with exact TFMs from selection, semicolon-separated)
  - TFM comparison: order-insensitive and case-insensitive for literals (e.g., net9;net8 equals net8;net9), exact match for variables
  - "Append target framework" button (enabled only when all selected rows are SDK-style)
  - Append target framework ComboBox (for entering TFMs to append)
  - **Append functionality:**
    - Works across mixed SDK projects with different existing TFM sets
    - Preserves existing TFMs in each project
    - Parses typed tokens (variables/literals) from input
    - Deduplicates tokens (variables exact match, literals case-insensitive)
    - Sorts tokens (variables first, then literals by version descending)
    - Writes to TargetFrameworks property (converts TargetFramework to TargetFrameworks when needed)
    - WinForms autocorrection: automatically adds `-windows` suffix to net5.0+ literal frameworks
    - Confirmation dialog before applying changes
    - Results dialog showing successful/failed updates
    - Updates grid with new TFM values
    - Highlights changed rows in green with checkmark
    - Disabled for Old-style projects
  - Empty TFM cells for projects without TargetFramework/TargetFrameworks
- **Responsive GUI:** Full screen support with proper anchoring for all controls
- Project style conversions region (placeholder)

### Planned
- Framework operations functionality (Change TFM)
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
  - Preserves variable tokens like `$(TargetFrameworks)` exactly as they appear
  - Returns empty string if no TFM property exists

### UI Features
- **Browse Button**: Opens folder browser dialog with memory of last path
- **Check Button**: Initiates async scan with real-time updates
- **Cancel Button**: Stops in-progress scans
- **Status Label**: Shows progress during scans
- **Button Heights**: Increased by 25% for better visibility
- **Responsive Layout**: All controls properly anchored for full-screen usage
- **DataGridView**: Expands to fill available space for viewing long paths

### Framework Operations
- **Selection-based Enablement**: The "Change target framework" button and ComboBox are only enabled when:
  - One or more rows are selected
  - All selected rows have identical TFM sets (compared using order-insensitive and case-insensitive logic for literals, exact match for variables)
- **TFM Comparison Rules**:
  - **Order-insensitive**: `net6.0;net7.0;net8.0` equals `net8.0;net7.0;net6.0`
  - **Case-insensitive for literals**: `NET6.0;NET7.0` equals `net6.0;net7.0`
  - **Exact match for variables**: `$(TargetFrameworks)` only matches `$(TargetFrameworks)` exactly
  - **Mixed sets**: Sets containing variables (like `net8.0;$(TargetFrameworks)`) require exact match
- **ComboBox Prefill**: When enabled, the ComboBox displays the exact TFMs from the selected rows (semicolon-separated), preserving the order and format from the csproj file
- **Empty TFMs**: Projects without TargetFramework/TargetFrameworks display an empty cell in the grid
