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

### Current (Step 8 - Polish and resilience)
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
  - **ComboBox Suggestions**: Populated with common TFMs (net9.0, net8.0, etc.) and discovered variables from scanned projects
  - TFM comparison: order-insensitive and case-insensitive for literals (e.g., net9;net8 equals net8;net9), exact match for variables
  - **Change TFM functionality:**
    - Replaces target framework(s) for selected projects
    - Works with both single and multiple targets
    - Handles TargetFramework ↔ TargetFrameworks conversion automatically
    - Removes conflicting TF/TFs elements
    - Confirmation dialog before applying changes
    - Results dialog showing successful/failed updates
    - Updates grid with new TFM values
    - Highlights changed rows in green with checkmark
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
- **Project Style Conversions:**
  - "Convert Old-style → SDK" button (enabled only when all selected rows are Old-style)
  - **Old-style → SDK Conversion functionality:**
    - Skips projects that are already SDK-style
    - Maps old-style framework versions to SDK-style (v4.x → net4x)
    - For WinForms projects, adds `-windows` suffix (e.g., v4.8 → net48-windows)
    - Preserves variable tokens verbatim (like `$(TargetFrameworks)`)
    - Converts project structure to SDK-style format
    - Removes legacy project elements (References, Imports, etc.)
    - Sets essential properties (OutputType, UseWindowsForms, ImplicitUsings, Nullable)
    - Confirmation dialog before applying changes
    - Results dialog showing successful/failed conversions
    - Updates grid with new Style and TFM values
    - Highlights changed rows in green with checkmark
  - "Convert SDK → Old-style" button (enabled only when all selected rows are SDK-style)
  - **SDK → Old-style Conversion functionality (constrained):**
    - Skips projects that are already Old-style
    - Only proceeds if single .NET Framework target (net40–net48[-windows]) and no PackageReference items
    - Blocks and reports projects with PackageReferences or multiple targets
    - Maps SDK-style framework versions to old-style (net4x → v4.x)
    - Preserves variable tokens verbatim (like `$(TargetFrameworks)`)
    - Converts project structure to Old-style format with MSBuild namespace
    - Adds legacy project elements (References, Imports, PropertyGroups)
    - Restores .NET Framework project structure
    - Confirmation dialog before applying changes
    - Results dialog showing successful/failed/skipped conversions
    - Updates grid with new Style and TFM values
    - Highlights changed rows in green with checkmark
- **Context Menu:**
  - Right-click on grid rows to access context menu
  - "Open containing folder" - Opens the project's folder in Windows Explorer
  - "Copy path" - Copies selected project path(s) to clipboard
  - Double-click on row to open .csproj file in default editor
- **Export to CSV:**
  - "Export CSV" button to export grid data
  - Generates timestamped CSV files
  - Properly escapes special characters (commas, quotes, newlines)
  - Includes all columns (Full Path, Style, Target Framework(s), Changed)
- **Robust XML Operations:**
  - Preserves original file encoding (UTF-8, UTF-8 with BOM, etc.)
  - Removes conflicting TargetFramework/TargetFrameworks elements
  - Minimizes file churn with consistent formatting
  - Handles read-only files gracefully with clear error messages
  - Handles locked files with appropriate error reporting
  - Proper indentation and XML declaration handling

### Planned
- None - all planned features implemented!

## Project Structure

- `CsprojChecker/` - Main WinForms application
  - `Program.cs` - Application entry point
  - `MainForm.cs` - Main application form with UI layout and scanning logic
- `docs/` - Documentation
  - `csproj-conversion-reference.md` - Comprehensive technical specification for Old-style ↔ SDK-style conversions
  - `conversion-validation.md` - Validation report proving implementation compliance
  - `conversion-quick-reference.md` - Quick reference guide for developers

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
- **Export CSV Button**: Exports current grid data to CSV file
- **Cancel Button**: Stops in-progress scans
- **Status Label**: Shows progress during scans and operation results
- **Button Heights**: Increased by 25% for better visibility
- **Responsive Layout**: All controls properly anchored for full-screen usage
- **DataGridView**: Expands to fill available space for viewing long paths
- **Context Menu**: Right-click on rows for "Open containing folder" and "Copy path" options
- **Double-Click**: Opens .csproj file in default editor

### Framework Operations
- **Selection-based Enablement**: The "Change target framework" button and ComboBox are only enabled when:
  - One or more rows are selected
  - All selected rows have identical TFM sets (compared using order-insensitive and case-insensitive logic for literals, exact match for variables)
- **ComboBox Suggestions**: Both target framework ComboBoxes are populated with:
  - Common TFMs: net9.0, net8.0, net7.0, net6.0, net5.0, netcoreapp3.1, netstandard2.1/2.0, net48-net45 (with and without -windows suffix)
  - Discovered variables: Any variables (e.g., `$(TargetFrameworks)`) found during project scanning are automatically added
- **TFM Comparison Rules**:
  - **Order-insensitive**: `net6.0;net7.0;net8.0` equals `net8.0;net7.0;net6.0`
  - **Case-insensitive for literals**: `NET6.0;NET7.0` equals `net6.0;net7.0`
  - **Exact match for variables**: `$(TargetFrameworks)` only matches `$(TargetFrameworks)` exactly
  - **Mixed sets**: Sets containing variables (like `net8.0;$(TargetFrameworks)`) require exact match
- **ComboBox Prefill**: When enabled, the "Change target framework" ComboBox displays the exact TFMs from the selected rows (semicolon-separated), preserving the order and format from the csproj file
- **Empty TFMs**: Projects without TargetFramework/TargetFrameworks display an empty cell in the grid

### Robust XML Operations
- **Encoding Preservation**: Detects and preserves original file encoding (UTF-8, UTF-8 with BOM, etc.)
- **Conflict Resolution**: Automatically removes conflicting TargetFramework/TargetFrameworks elements when switching between single and multiple targets
- **Formatting**: Maintains consistent XML formatting with proper indentation (2 spaces)
- **Error Handling**:
  - Detects and reports read-only files with clear error messages
  - Handles locked files gracefully with appropriate error reporting
  - Specific exception handling for file access issues vs. other errors
- **Minimal Churn**: Preserves whitespace and declaration settings to minimize unnecessary file changes

## Documentation

For detailed information about the .csproj conversion logic:

- **[Conversion Reference](docs/csproj-conversion-reference.md)** - Comprehensive technical specification defining all Old-style ↔ SDK-style conversion mappings, constraints, and test cases
- **[Conversion Validation](docs/conversion-validation.md)** - Validation report demonstrating 100% compliance with the conversion reference
- **[Quick Reference](docs/conversion-quick-reference.md)** - Developer quick reference guide with examples and common use cases
