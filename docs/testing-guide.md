# .csproj Conversion Testing Guide

This guide explains how to manually test and validate the .csproj conversion functionality against the specifications in [`csproj-conversion-reference.md`](csproj-conversion-reference.md).

## Purpose

This guide helps developers and reviewers:
- Validate that conversion logic works as documented
- Test changes to conversion code
- Verify bug fixes don't break existing functionality
- Ensure new features comply with the conversion reference

## Prerequisites

- CsProjConverter application built and ready to run
- Sample .csproj files (or ability to create them)
- Access to the conversion reference documentation

## Testing Approach

### 1. Automated Reference Testing

The conversion reference includes 7 comprehensive test cases in Section 10. Use these as your baseline:

1. **Test Case 1**: Old-style Console App (net48) → SDK-style
2. **Test Case 2**: Old-style WinForms App (net472) → SDK-style  
3. **Test Case 3**: SDK-style Console App (net48) → Old-style
4. **Test Case 4**: SDK-style with PackageReference → Old-style (BLOCKED)
5. **Test Case 5**: SDK-style with Multi-Targeting → Old-style (BLOCKED)
6. **Test Case 6**: Variable Token Preservation (Old→SDK)
7. **Test Case 7**: Variable Token Preservation (SDK→Old)

### 2. Manual Testing Workflow

#### Setup Test Environment

1. Create a test folder structure:
   ```
   TestProjects/
   ├── OldStyle/
   │   ├── ConsoleApp/
   │   ├── WinFormsApp/
   │   └── LibraryApp/
   └── SdkStyle/
       ├── ConsoleApp/
       ├── WinFormsApp/
       └── LibraryApp/
   ```

2. Create sample .csproj files for each type (use test cases from reference)

#### Test Old-style → SDK-style Conversion

**Test 1: Basic Console Application (net48)**

1. Create `OldStyleConsole.csproj`:
   ```xml
   <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003" ToolsVersion="15.0">
     <PropertyGroup>
       <OutputType>Exe</OutputType>
       <TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
       <RootNamespace>ConsoleApp1</RootNamespace>
       <AssemblyName>ConsoleApp1</AssemblyName>
     </PropertyGroup>
     <ItemGroup>
       <Reference Include="System" />
     </ItemGroup>
     <Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />
   </Project>
   ```

2. Load in CsProjConverter and verify detection:
   - Style: "Old-style"
   - Target Framework: "v4.8"

3. Select and click "Convert Old-style → SDK"

4. Verify output matches expected:
   - Has `Sdk="Microsoft.NET.Sdk"` attribute
   - Has `<TargetFramework>net48</TargetFramework>`
   - Has `<ImplicitUsings>enable</ImplicitUsings>`
   - Has `<Nullable>enable</Nullable>`
   - No xmlns attribute
   - No Import statements
   - Preserved OutputType, RootNamespace, AssemblyName

**Test 2: WinForms Application (net472)**

1. Create `OldStyleWinForms.csproj`:
   ```xml
   <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003" ToolsVersion="15.0">
     <PropertyGroup>
       <OutputType>WinExe</OutputType>
       <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
       <RootNamespace>WinFormsApp1</RootNamespace>
       <AssemblyName>WinFormsApp1</AssemblyName>
     </PropertyGroup>
     <ItemGroup>
       <Reference Include="System" />
       <Reference Include="System.Windows.Forms" />
       <Reference Include="System.Drawing" />
     </ItemGroup>
     <Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />
   </Project>
   ```

2. Load and verify detection

3. Convert and verify output:
   - ✅ Has `<TargetFramework>net472-windows</TargetFramework>` (with -windows suffix)
   - ✅ Has `<UseWindowsForms>true</UseWindowsForms>`
   - ✅ No explicit System.Windows.Forms/Drawing references

**Test 3: Variable Token Preservation**

1. Create project with `<TargetFrameworkVersion>$(MyCustomFramework)</TargetFrameworkVersion>`

2. Convert and verify:
   - ✅ Output has `<TargetFramework>$(MyCustomFramework)</TargetFramework>`
   - ✅ Variable preserved exactly (not converted)

#### Test SDK-style → Old-style Conversion

**Test 4: Basic Console Application (net48)**

1. Create `SdkStyleConsole.csproj`:
   ```xml
   <Project Sdk="Microsoft.NET.Sdk">
     <PropertyGroup>
       <TargetFramework>net48</TargetFramework>
       <OutputType>Exe</OutputType>
       <RootNamespace>ConsoleApp1</RootNamespace>
       <AssemblyName>ConsoleApp1</AssemblyName>
     </PropertyGroup>
   </Project>
   ```

2. Convert and verify output has:
   - ✅ `xmlns="http://schemas.microsoft.com/developer/msbuild/2003"`
   - ✅ `ToolsVersion="15.0"`
   - ✅ `<Import>` for Microsoft.Common.props (at beginning)
   - ✅ `<Import>` for Microsoft.CSharp.targets (at end)
   - ✅ `<ProjectGuid>` (newly generated)
   - ✅ `<Configuration>` and `<Platform>` defaults
   - ✅ `<FileAlignment>512</FileAlignment>`
   - ✅ `<Deterministic>true</Deterministic>`
   - ✅ Debug PropertyGroup with DebugSymbols, DebugType, Optimize, OutputPath, DefineConstants, ErrorReport, WarningLevel
   - ✅ Release PropertyGroup
   - ✅ Standard references (System, System.Core, System.Xml.Linq, System.Data.DataSetExtensions, Microsoft.CSharp, System.Data, System.Xml)
   - ✅ `<TargetFrameworkVersion>v4.8</TargetFrameworkVersion>`

**Test 5: Blocked Conversions**

Create projects that should be blocked:

1. **With PackageReference**:
   ```xml
   <Project Sdk="Microsoft.NET.Sdk">
     <PropertyGroup>
       <TargetFramework>net48</TargetFramework>
       <OutputType>Exe</OutputType>
     </PropertyGroup>
     <ItemGroup>
       <PackageReference Include="Newtonsoft.Json" Version="13.0.1" />
     </ItemGroup>
   </Project>
   ```
   - ✅ Conversion blocked with message "Has 1 PackageReference(s)"

2. **With Multi-Targeting**:
   ```xml
   <Project Sdk="Microsoft.NET.Sdk">
     <PropertyGroup>
       <TargetFrameworks>net48;net6.0</TargetFrameworks>
       <OutputType>Exe</OutputType>
     </PropertyGroup>
   </Project>
   ```
   - ✅ Conversion blocked with message "Multiple target frameworks"

3. **With Non-.NET Framework Target**:
   ```xml
   <Project Sdk="Microsoft.NET.Sdk">
     <PropertyGroup>
       <TargetFramework>net6.0</TargetFramework>
       <OutputType>Exe</OutputType>
     </PropertyGroup>
   </Project>
   ```
   - ✅ Conversion blocked with message "Not a .NET Framework target"

### 3. Framework Version Testing

Test all supported framework versions:

**Old-style → SDK-style**:
- v4.8 → net48 ✅
- v4.7.2 → net472 ✅
- v4.7.1 → net471 ✅
- v4.7 → net47 ✅
- v4.6.2 → net462 ✅
- v4.6.1 → net461 ✅
- v4.6 → net46 ✅
- v4.5.2 → net452 ✅
- v4.5.1 → net451 ✅
- v4.5 → net45 ✅
- v4.0 → net40 ✅
- v4.0.3 → net403 ✅

**SDK-style → Old-style**:
- net48 → v4.8 ✅
- net48-windows → v4.8 ✅ (strips -windows)
- net472 → v4.7.2 ✅
- (test all other versions similarly)

### 4. Edge Case Testing

**Encoding Preservation**:
1. Create .csproj with UTF-8 with BOM
2. Convert it
3. Verify encoding is preserved (check with hex editor or text editor that shows encoding)

**Read-only Files**:
1. Create .csproj
2. Mark as read-only
3. Attempt conversion
4. Verify clear error message: "File is read-only"

**Locked Files**:
1. Create .csproj
2. Open in another application (e.g., text editor with file lock)
3. Attempt conversion
4. Verify clear error message about file being locked

**Invalid XML**:
1. Create malformed .csproj
2. Attempt conversion
3. Verify appropriate error handling

### 5. Properties Preservation Testing

**Properties that should be preserved**:
- OutputType
- RootNamespace
- AssemblyName
- Custom properties (if any)

**Properties that should be added (Old→SDK)**:
- ImplicitUsings
- Nullable
- UseWindowsForms (if WinForms)

**Properties that should be omitted (Old→SDK)**:
- ProjectGuid
- Configuration
- Platform
- FileAlignment
- Debug/Release configurations
- DebugSymbols, DebugType, Optimize, OutputPath, DefineConstants, ErrorReport, WarningLevel

**Properties that should be added (SDK→Old)**:
- ProjectGuid (newly generated)
- Configuration and Platform defaults
- FileAlignment
- Deterministic
- Debug PropertyGroup
- Release PropertyGroup

**Properties that should be omitted (SDK→Old)**:
- ImplicitUsings
- Nullable
- UseWindowsForms (converted to references)

### 6. References Testing

**Old-style → SDK-style**:
- System, System.Core, System.Data, System.Xml → removed (implicit)
- System.Windows.Forms, System.Drawing → removed (implicit with UseWindowsForms)
- Third-party DLL references → preserved

**SDK-style → Old-style**:
- Implicit → System, System.Core, System.Xml.Linq, System.Data.DataSetExtensions, Microsoft.CSharp, System.Data, System.Xml added
- UseWindowsForms=true → System.Windows.Forms and System.Drawing added
- Explicit references → preserved

## Validation Checklist

Use [`conversion-checklist.md`](conversion-checklist.md) as a compact checklist during testing.

## Reporting Issues

When reporting conversion bugs or issues:
1. Include the source .csproj (before conversion)
2. Include the actual output .csproj (after conversion)
3. Include the expected output (from reference documentation)
4. Specify which test case or scenario failed
5. Include error messages (if any)
6. Specify CsProjConverter version and environment

## Continuous Validation

When making changes to conversion code:
1. Run through all 7 test cases from the reference
2. Test all framework versions
3. Test edge cases (encoding, read-only, locked files)
4. Verify blocking conditions work correctly
5. Check that error messages are clear and actionable

## Automation Opportunities

While this is currently a manual testing guide, consider automating:
- Creating test .csproj files programmatically
- Parsing and comparing XML structures
- Running conversion and validating output
- Regression testing for known issues

## See Also

- [`csproj-conversion-reference.md`](csproj-conversion-reference.md) - Full technical specification
- [`conversion-checklist.md`](conversion-checklist.md) - Quick validation checklist
- [`conversion-quick-reference.md`](conversion-quick-reference.md) - Developer quick reference
- [`conversion-validation.md`](conversion-validation.md) - Implementation compliance report
