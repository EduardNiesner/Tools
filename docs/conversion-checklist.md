# .csproj Conversion Validation Checklist

Quick checklist for validating .csproj conversions against [`csproj-conversion-reference.md`](csproj-conversion-reference.md).

## Old-style → SDK-style Conversion

### Root Element
- [ ] `<Project xmlns="..." ToolsVersion="...">` → `<Project Sdk="Microsoft.NET.Sdk">`
- [ ] All `<Import>` statements removed

### Framework Version
- [ ] `<TargetFrameworkVersion>v4.8</TargetFrameworkVersion>` → `<TargetFramework>net48</TargetFramework>`
- [ ] Version format correct: v4.7.2 → net472, v4.5.1 → net451, etc.
- [ ] WinForms projects have `-windows` suffix: net48-windows
- [ ] Variable tokens preserved verbatim: $(Var) → $(Var)

### Properties - Preserved
- [ ] OutputType preserved
- [ ] RootNamespace preserved
- [ ] AssemblyName preserved

### Properties - Added
- [ ] `<ImplicitUsings>enable</ImplicitUsings>` added
- [ ] `<Nullable>enable</Nullable>` added
- [ ] `<UseWindowsForms>true</UseWindowsForms>` added if WinForms detected

### Properties - Removed
- [ ] ProjectGuid removed
- [ ] Configuration removed
- [ ] Platform removed
- [ ] FileAlignment removed
- [ ] Debug PropertyGroup removed (DebugSymbols, DebugType, Optimize, OutputPath, DefineConstants, ErrorReport, WarningLevel)
- [ ] Release PropertyGroup removed

### References
- [ ] System references removed (implicit)
- [ ] System.Windows.Forms and System.Drawing removed if WinForms
- [ ] Third-party explicit references preserved

### File Items
- [ ] Compile items removed (implicit)
- [ ] EmbeddedResource items removed (implicit)
- [ ] None items removed if standard

---

## SDK-style → Old-style Conversion

### Validation Before Conversion
- [ ] Project is SDK-style (has Sdk attribute)
- [ ] No PackageReference items → **BLOCKS** conversion
- [ ] Single target framework → **BLOCKS** if TargetFrameworks plural
- [ ] .NET Framework target (net40-net48) → **BLOCKS** if net5.0+, netstandard, netcoreapp
- [ ] Variable tokens allowed (preserved verbatim)

### Root Element
- [ ] `<Project Sdk="Microsoft.NET.Sdk">` → `<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003" ToolsVersion="15.0">`
- [ ] `<Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props" Condition="Exists(...)" />` added at beginning
- [ ] `<Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />` added at end

### Framework Version
- [ ] `<TargetFramework>net48</TargetFramework>` → `<TargetFrameworkVersion>v4.8</TargetFrameworkVersion>`
- [ ] Version format correct: net472 → v4.7.2, net451 → v4.5.1, etc.
- [ ] `-windows` suffix stripped: net48-windows → v4.8
- [ ] Variable tokens preserved verbatim: $(Var) → $(Var)

### Properties - Preserved
- [ ] OutputType preserved
- [ ] RootNamespace preserved (or inferred from filename if missing)
- [ ] AssemblyName preserved (or inferred from filename if missing)

### Properties - Added
- [ ] `<ProjectGuid>{NEW-GUID}</ProjectGuid>` generated
- [ ] `<Configuration Condition="'$(Configuration)'==''">Debug</Configuration>` added
- [ ] `<Platform Condition="'$(Platform)'==''">AnyCPU</Platform>` added
- [ ] `<FileAlignment>512</FileAlignment>` added
- [ ] `<Deterministic>true</Deterministic>` added

### PropertyGroups - Added
- [ ] Debug PropertyGroup with Condition="'$(Configuration)|$(Platform)' == 'Debug|AnyCPU'"
  - [ ] DebugSymbols=true
  - [ ] DebugType=full
  - [ ] Optimize=false
  - [ ] OutputPath=bin\Debug\
  - [ ] DefineConstants=DEBUG;TRACE
  - [ ] ErrorReport=prompt
  - [ ] WarningLevel=4

- [ ] Release PropertyGroup with Condition="'$(Configuration)|$(Platform)' == 'Release|AnyCPU'"
  - [ ] DebugType=pdbonly
  - [ ] Optimize=true
  - [ ] OutputPath=bin\Release\
  - [ ] DefineConstants=TRACE
  - [ ] ErrorReport=prompt
  - [ ] WarningLevel=4

### Properties - Removed
- [ ] ImplicitUsings removed
- [ ] Nullable removed
- [ ] UseWindowsForms removed (converted to references)

### References - Added
- [ ] `<Reference Include="System" />`
- [ ] `<Reference Include="System.Core" />`
- [ ] `<Reference Include="System.Xml.Linq" />`
- [ ] `<Reference Include="System.Data.DataSetExtensions" />`
- [ ] `<Reference Include="Microsoft.CSharp" />`
- [ ] `<Reference Include="System.Data" />`
- [ ] `<Reference Include="System.Xml" />`
- [ ] `<Reference Include="System.Windows.Forms" />` if UseWindowsForms was true
- [ ] `<Reference Include="System.Drawing" />` if UseWindowsForms was true

### References - Preserved
- [ ] Explicit third-party references preserved

---

## Both Directions

### Encoding
- [ ] Original file encoding preserved (UTF-8, UTF-8 with BOM, etc.)
- [ ] XML declaration preserved if present

### Error Handling
- [ ] Read-only files detected with clear error message
- [ ] Locked files detected with clear error message
- [ ] Invalid XML handled gracefully

### Variable Tokens
- [ ] Variables starting with $ preserved exactly as-is
- [ ] No conversion attempted on variables

### WinForms Detection
- [ ] Detected by System.Windows.Forms reference, OR
- [ ] Detected by System.Drawing reference, OR
- [ ] Detected by UseWindowsForms property, OR
- [ ] Detected by .Designer.cs files

---

## Blocking Conditions (SDK → Old-style)

### PackageReference Check
- [ ] Scans for `<PackageReference>` items
- [ ] Blocks with message: "Has N PackageReference(s)"
- [ ] Provides clear error in results dialog

### Multi-Targeting Check
- [ ] Checks for `<TargetFrameworks>` (plural) with semicolons
- [ ] Blocks with message: "Multiple target frameworks"
- [ ] Variables are allowed (checked separately)

### Framework Target Check
- [ ] Validates target is .NET Framework (net40-net48)
- [ ] Blocks net5.0+, netstandard, netcoreapp
- [ ] Message: "Not a .NET Framework target"

### Already Converted Check
- [ ] Detects if already Old-style (has xmlns, no Sdk)
- [ ] Skips with message: "Already Old-style"

---

## Test Cases (from Reference Section 10)

- [ ] Test Case 1: Old-style Console App (net48) → SDK-style ✅
- [ ] Test Case 2: Old-style WinForms App (net472) → SDK-style ✅
- [ ] Test Case 3: SDK-style Console App (net48) → Old-style ✅
- [ ] Test Case 4: SDK with PackageReference → Old-style (BLOCKED) ✅
- [ ] Test Case 5: SDK with Multi-Targeting → Old-style (BLOCKED) ✅
- [ ] Test Case 6: Variable Token Preservation (Old→SDK) ✅
- [ ] Test Case 7: Variable Token Preservation (SDK→Old) ✅

---

## Framework Versions

### Old-style → SDK-style Mappings
- [ ] v4.8 → net48
- [ ] v4.7.2 → net472
- [ ] v4.7.1 → net471
- [ ] v4.7 → net47
- [ ] v4.6.2 → net462
- [ ] v4.6.1 → net461
- [ ] v4.6 → net46
- [ ] v4.5.2 → net452
- [ ] v4.5.1 → net451
- [ ] v4.5 → net45
- [ ] v4.0 → net40
- [ ] v4.0.3 → net403

### SDK-style → Old-style Mappings
- [ ] net48 → v4.8
- [ ] net48-windows → v4.8 (strips suffix)
- [ ] net472 → v4.7.2
- [ ] net471 → v4.7.1
- [ ] net47 → v4.7
- [ ] net462 → v4.6.2
- [ ] net461 → v4.6.1
- [ ] net46 → v4.6
- [ ] net452 → v4.5.2
- [ ] net451 → v4.5.1
- [ ] net45 → v4.5
- [ ] net40 → v4.0
- [ ] net403 → v4.0.3

---

## UI Validation

### Conversion Buttons
- [ ] "Convert Old-style → SDK" enabled only for Old-style selections
- [ ] "Convert SDK → Old-style" enabled only for SDK-style selections
- [ ] Confirmation dialog shown before conversion
- [ ] Results dialog shows success/failure/skipped counts

### Grid Updates
- [ ] Style column updated after conversion
- [ ] Target Framework column updated with new format
- [ ] Changed rows highlighted in green with checkmark
- [ ] Grid refreshes properly

### Status Messages
- [ ] Clear success messages
- [ ] Clear error messages for blocks
- [ ] Clear error messages for file issues
- [ ] Actionable guidance in error messages

---

## Documentation Cross-Reference

- [ ] Conversion follows [`csproj-conversion-reference.md`](csproj-conversion-reference.md) exactly
- [ ] No undocumented conversions performed
- [ ] All documented conversions implemented
- [ ] Constraints match documentation

---

**Use this checklist**:
- During code reviews of conversion logic changes
- When testing conversion functionality
- When validating bug fixes
- When adding new conversion features
- As acceptance criteria for conversion-related PRs

**See also**:
- [`csproj-conversion-reference.md`](csproj-conversion-reference.md) - Complete specification
- [`testing-guide.md`](testing-guide.md) - Detailed testing procedures
- [`conversion-quick-reference.md`](conversion-quick-reference.md) - Quick developer guide
- [`conversion-validation.md`](conversion-validation.md) - Implementation compliance report
