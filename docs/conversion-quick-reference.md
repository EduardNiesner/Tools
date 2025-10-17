# .csproj Conversion Quick Reference

A quick guide for developers using or maintaining the CsprojChecker conversion features.

## Supported Conversions

### ✅ Old-style → SDK-style
- **Console apps** (OutputType: Exe)
- **WinForms apps** (OutputType: WinExe)
- **Libraries** (OutputType: Library)
- **Any .NET Framework version** (v4.0 - v4.8.1)
- **Projects with variables** (e.g., `$(TargetFrameworks)`)

### ✅ SDK-style → Old-style (Constrained)
- **Single .NET Framework target** (net40 - net48)
- **No PackageReference items** (packages.config only)
- **Console, WinForms, or Library projects**
- **Projects with variables** (preserved verbatim)

### ❌ SDK-style → Old-style (Blocked)
- Projects with `<PackageReference>` items
- Multi-target projects (`<TargetFrameworks>` with semicolons)
- Non-.NET Framework targets (net5.0+, netstandard, netcoreapp)

## Framework Version Mappings

### Common Mappings
| Old-style | SDK-style | SDK-style (WinForms) |
|-----------|-----------|----------------------|
| v4.8 | net48 | net48-windows |
| v4.7.2 | net472 | net472-windows |
| v4.7.1 | net471 | net471-windows |
| v4.7 | net47 | net47-windows |
| v4.6.2 | net462 | net462-windows |
| v4.6.1 | net461 | net461-windows |
| v4.6 | net46 | net46-windows |
| v4.5.2 | net452 | net452-windows |
| v4.5.1 | net451 | net451-windows |
| v4.5 | net45 | net45-windows |

### Variable Tokens
Variables like `$(TargetFrameworks)` are **always preserved verbatim** in both directions.

## What Changes During Conversion

### Old-style → SDK-style Changes
**Removed**:
- `xmlns` and `ToolsVersion` attributes
- `<Import>` statements
- `<ProjectGuid>`
- `<Configuration>` and `<Platform>` defaults
- `<FileAlignment>`
- Debug/Release `<PropertyGroup>` sections
- `<Reference>` items for framework assemblies
- `<Compile>` and other file items (now implicit)

**Added**:
- `Sdk="Microsoft.NET.Sdk"` attribute
- `<ImplicitUsings>enable</ImplicitUsings>`
- `<Nullable>enable</Nullable>`
- `<UseWindowsForms>true</UseWindowsForms>` (if WinForms detected)

**Preserved**:
- `<OutputType>`
- `<RootNamespace>`
- `<AssemblyName>`
- `<TargetFramework[Version]>` (converted)
- Explicit `<Reference>` items (non-framework)

### SDK-style → Old-style Changes
**Removed**:
- `Sdk` attribute
- `<ImplicitUsings>`
- `<Nullable>`
- `<UseWindowsForms>` (inferred from added references)

**Added**:
- `xmlns` and `ToolsVersion` attributes
- `<Import>` for Microsoft.Common.props
- `<Import>` for Microsoft.CSharp.targets
- `<ProjectGuid>` (newly generated)
- `<Configuration>` and `<Platform>` defaults
- `<FileAlignment>512</FileAlignment>`
- `<Deterministic>true</Deterministic>`
- Debug `<PropertyGroup>` (DebugSymbols, DebugType, Optimize, etc.)
- Release `<PropertyGroup>` (DebugType, Optimize, etc.)
- `<Reference>` items for System, System.Core, System.Xml, etc.
- WinForms references if needed (System.Windows.Forms, System.Drawing)

**Preserved**:
- `<OutputType>`
- `<RootNamespace>` (or inferred from filename)
- `<AssemblyName>` (or inferred from filename)
- `<TargetFramework[Version]>` (converted)
- Explicit `<Reference>` items (non-framework)

**NOT Added** (User Responsibility):
- `<Compile>` items - user must add file references manually

## WinForms Detection

A project is considered WinForms if it has:
1. Reference to `System.Windows.Forms`, OR
2. Reference to `System.Drawing`, OR
3. `<UseWindowsForms>true</UseWindowsForms>`, OR
4. Files ending in `.Designer.cs`

When detected, conversion adds:
- SDK-style: `<UseWindowsForms>true</UseWindowsForms>` and `-windows` suffix
- Old-style: References to System.Windows.Forms and System.Drawing

## Validation and Error Messages

### SDK → Old-style Validation Errors
| Error Message | Meaning | Solution |
|---------------|---------|----------|
| "Already Old-style" | Project is already old-style | No conversion needed |
| "Has N PackageReference(s)" | Project uses NuGet PackageReference | Remove PackageReferences or convert to packages.config first |
| "Multiple target frameworks" | Project multi-targets | Remove all but one target framework |
| "Not a .NET Framework target" | Project targets net5.0+, netstandard, or netcoreapp | Change to single .NET Framework target (net40-net48) |
| "Invalid project file" | XML parsing failed | Fix XML syntax errors |

### File Access Errors
| Error Message | Meaning | Solution |
|---------------|---------|----------|
| "File is read-only" | File has read-only attribute | Remove read-only attribute |
| "File is locked or inaccessible" | Another process has file open | Close other applications using the file |
| "Failed to write to file (may be locked)" | Write operation failed | Check file permissions and locks |

## Example Conversions

### Example 1: Simple Console App
**Before (Old-style)**:
```xml
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003" ToolsVersion="15.0">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
  </PropertyGroup>
  <Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />
</Project>
```

**After (SDK-style)**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <OutputType>Exe</OutputType>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

### Example 2: WinForms App
**Before (Old-style)**:
```xml
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="System.Windows.Forms" />
    <Reference Include="System.Drawing" />
  </ItemGroup>
</Project>
```

**After (SDK-style)**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net472-windows</TargetFramework>
    <OutputType>WinExe</OutputType>
    <UseWindowsForms>true</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

## Important Notes

### Round-Trip Safety
⚠️ **Conversions are NOT round-trip safe**. Converting Old→SDK→Old or SDK→Old→SDK will not restore the exact original file due to:
- Simplifications in SDK-style (implicit defaults)
- Generated values in Old-style (new ProjectGuid)
- Omitted file items in both directions

### File Items
⚠️ **File items (`<Compile>`, etc.) are not added** during conversion:
- SDK-style automatically includes `**/*.cs` files
- Old-style requires explicit `<Compile>` items, but the tool doesn't enumerate them
- Users must manually add file items to Old-style projects after conversion

### Package Management
⚠️ **Package conversion is NOT automatic**:
- Old-style uses `packages.config`
- SDK-style uses `<PackageReference>`
- Projects with PackageReference are blocked from SDK→Old conversion
- Users must manually migrate packages when converting Old→SDK

### Encoding Preservation
✅ The tool preserves the original file encoding (UTF-8, UTF-8 with BOM, etc.) during conversion.

## For Developers

### Key Methods
- `ConvertOldStyleToSdkAsync()` - Main Old→SDK conversion
- `ConvertSdkToOldStyleAsync()` - Main SDK→Old conversion
- `ConvertFrameworkVersion()` - Framework version Old→SDK
- `ConvertSdkToOldStyleFrameworkVersion()` - Framework version SDK→Old
- `ValidateSdkToOldStyleConstraints()` - Pre-conversion validation
- `DetectWinFormsProject()` - WinForms detection (Old-style)
- `IsWinFormsInSdkProject()` - WinForms detection (SDK-style)

### Testing
See `docs/csproj-conversion-reference.md` section 10 for comprehensive test cases.

### Implementation Location
All conversion logic is in `CsprojChecker/MainForm.cs`, lines 1430-1952.

---

**For full technical details**, see `docs/csproj-conversion-reference.md`  
**For validation report**, see `docs/conversion-validation.md`
