# .csproj Conversion Reference: Old-style ↔ SDK-style

## Purpose
- Provide a practical, testable mapping for converting between Old-style (.NET Framework-era, explicit imports and items) and SDK-style (.NET SDK, implicit defaults).
- Focus on elements where conversion makes sense for desktop/server C# projects.
- Call out non-mappable or lossy areas so round-trip expectations are clear.

## Scope
- **Language**: C#
- **Project types**: Console/WinExe/Library; WinForms/WPF (desktop)
- **Frameworks**: .NET Framework (net2–net48) and modern .NET (net5+)
- **Excludes**: uncommon or legacy-specific properties not typically used today (unless needed for conversion)

## Conventions
- **Old-style** = project with `<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">`, explicit `<Import>` statements, verbose PropertyGroups
- **SDK-style** = project with `<Project Sdk="Microsoft.NET.Sdk">`, implicit defaults, minimal XML
- **Variable tokens** = MSBuild variables like `$(TargetFrameworks)` — always preserved verbatim during conversion
- **Round-trip** = converting Old→SDK→Old or SDK→Old→SDK may not restore the exact original due to simplifications and defaults

---

## 1. Root Element

### Old-style → SDK-style
| Old-style | SDK-style | Notes |
|-----------|-----------|-------|
| `<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003" ToolsVersion="...">` | `<Project Sdk="Microsoft.NET.Sdk">` | SDK attribute replaces xmlns and ToolsVersion |
| `<Import Project="$(MSBuildExtensionsPath)\...">` | (implicit) | Common imports are implicit in SDK-style |
| `<Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />` | (implicit) | Implicit in SDK-style |

### SDK-style → Old-style
| SDK-style | Old-style | Notes |
|-----------|-----------|-------|
| `<Project Sdk="Microsoft.NET.Sdk">` | `<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003" ToolsVersion="15.0">` | Add xmlns and ToolsVersion |
| (no imports) | `<Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props" Condition="Exists(...)" />` | Add at beginning |
| (no imports) | `<Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />` | Add at end |

---

## 2. Target Framework

### Old-style → SDK-style
| Old-style | SDK-style | Notes |
|-----------|-----------|-------|
| `<TargetFrameworkVersion>v4.8</TargetFrameworkVersion>` | `<TargetFramework>net48</TargetFramework>` | Convert v4.x → net4x |
| `<TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>` | `<TargetFramework>net472</TargetFramework>` | Remove dots from version |
| `<TargetFrameworkVersion>v4.7.1</TargetFrameworkVersion>` | `<TargetFramework>net471</TargetFramework>` | |
| `<TargetFrameworkVersion>v4.7</TargetFrameworkVersion>` | `<TargetFramework>net47</TargetFramework>` | |
| `<TargetFrameworkVersion>v4.6.2</TargetFrameworkVersion>` | `<TargetFramework>net462</TargetFramework>` | |
| `<TargetFrameworkVersion>v4.6.1</TargetFrameworkVersion>` | `<TargetFramework>net461</TargetFramework>` | |
| `<TargetFrameworkVersion>v4.6</TargetFrameworkVersion>` | `<TargetFramework>net46</TargetFramework>` | |
| `<TargetFrameworkVersion>v4.5.2</TargetFrameworkVersion>` | `<TargetFramework>net452</TargetFramework>` | |
| `<TargetFrameworkVersion>v4.5.1</TargetFrameworkVersion>` | `<TargetFramework>net451</TargetFramework>` | |
| `<TargetFrameworkVersion>v4.5</TargetFrameworkVersion>` | `<TargetFramework>net45</TargetFramework>` | |
| `<TargetFrameworkVersion>v4.0</TargetFrameworkVersion>` | `<TargetFramework>net40</TargetFramework>` | |
| `<TargetFrameworkVersion>v4.0.3</TargetFrameworkVersion>` | `<TargetFramework>net403</TargetFramework>` | |
| `<TargetFrameworkVersion>$(SomeVariable)</TargetFrameworkVersion>` | `<TargetFramework>$(SomeVariable)</TargetFramework>` | Variables preserved verbatim |

**WinForms Projects**: Add `-windows` suffix (e.g., `net48-windows`) when converting from Old-style WinForms projects.

### SDK-style → Old-style
| SDK-style | Old-style | Notes |
|-----------|-----------|-------|
| `<TargetFramework>net48</TargetFramework>` | `<TargetFrameworkVersion>v4.8</TargetFrameworkVersion>` | Convert net4x → v4.x |
| `<TargetFramework>net48-windows</TargetFramework>` | `<TargetFrameworkVersion>v4.8</TargetFrameworkVersion>` | Strip -windows suffix |
| `<TargetFramework>net472</TargetFramework>` | `<TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>` | Add dots to version |
| `<TargetFramework>net471</TargetFramework>` | `<TargetFrameworkVersion>v4.7.1</TargetFrameworkVersion>` | |
| `<TargetFramework>net47</TargetFramework>` | `<TargetFrameworkVersion>v4.7</TargetFrameworkVersion>` | |
| `<TargetFramework>net462</TargetFramework>` | `<TargetFrameworkVersion>v4.6.2</TargetFrameworkVersion>` | |
| `<TargetFramework>net461</TargetFramework>` | `<TargetFrameworkVersion>v4.6.1</TargetFrameworkVersion>` | |
| `<TargetFramework>net46</TargetFramework>` | `<TargetFrameworkVersion>v4.6</TargetFrameworkVersion>` | |
| `<TargetFramework>net452</TargetFramework>` | `<TargetFrameworkVersion>v4.5.2</TargetFrameworkVersion>` | |
| `<TargetFramework>net451</TargetFramework>` | `<TargetFrameworkVersion>v4.5.1</TargetFrameworkVersion>` | |
| `<TargetFramework>net45</TargetFramework>` | `<TargetFrameworkVersion>v4.5</TargetFrameworkVersion>` | |
| `<TargetFramework>net40</TargetFramework>` | `<TargetFrameworkVersion>v4.0</TargetFrameworkVersion>` | |
| `<TargetFramework>net403</TargetFramework>` | `<TargetFrameworkVersion>v4.0.3</TargetFrameworkVersion>` | |
| `<TargetFramework>$(SomeVariable)</TargetFramework>` | `<TargetFrameworkVersion>$(SomeVariable)</TargetFrameworkVersion>` | Variables preserved verbatim |
| `<TargetFrameworks>net6.0;net8.0</TargetFrameworks>` | ❌ NOT SUPPORTED | Multi-targeting blocked |
| `<TargetFramework>net6.0</TargetFramework>` | ❌ NOT SUPPORTED | Non-.NET Framework targets blocked |

**Constraint**: SDK→Old-style conversion only supports single .NET Framework targets (net40–net48, with or without -windows).

---

## 3. Common Properties

### Old-style → SDK-style
| Old-style Property | SDK-style Property | Notes |
|-------------------|-------------------|-------|
| `<OutputType>WinExe</OutputType>` | `<OutputType>WinExe</OutputType>` | Preserved as-is |
| `<OutputType>Exe</OutputType>` | `<OutputType>Exe</OutputType>` | Preserved as-is |
| `<OutputType>Library</OutputType>` | `<OutputType>Library</OutputType>` | Preserved as-is |
| `<RootNamespace>MyApp</RootNamespace>` | `<RootNamespace>MyApp</RootNamespace>` | Preserved if present |
| `<AssemblyName>MyApp</AssemblyName>` | `<AssemblyName>MyApp</AssemblyName>` | Preserved if present |
| `<FileAlignment>512</FileAlignment>` | (omitted) | Default in SDK-style |
| `<ProjectGuid>{...}</ProjectGuid>` | (omitted) | Not needed in SDK-style |
| `<Configuration Condition="...">Debug</Configuration>` | (omitted) | Implicit in SDK-style |
| `<Platform Condition="...">AnyCPU</Platform>` | (omitted) | Implicit in SDK-style |
| `<DebugSymbols>true</DebugSymbols>` | (omitted) | Controlled by configuration |
| `<DebugType>full</DebugType>` | (omitted) | Controlled by configuration |
| `<Optimize>false</Optimize>` | (omitted) | Controlled by configuration |
| `<OutputPath>bin\Debug\</OutputPath>` | (omitted) | Default in SDK-style |
| `<DefineConstants>DEBUG;TRACE</DefineConstants>` | (omitted) | Default in SDK-style |
| `<ErrorReport>prompt</ErrorReport>` | (omitted) | Default in SDK-style |
| `<WarningLevel>4</WarningLevel>` | (omitted) | Default in SDK-style |
| (none) | `<ImplicitUsings>enable</ImplicitUsings>` | **Added** in SDK-style |
| (none) | `<Nullable>enable</Nullable>` | **Added** in SDK-style |

### SDK-style → Old-style
| SDK-style Property | Old-style Property | Notes |
|-------------------|-------------------|-------|
| `<OutputType>WinExe</OutputType>` | `<OutputType>WinExe</OutputType>` | Preserved |
| `<OutputType>Exe</OutputType>` | `<OutputType>Exe</OutputType>` | Preserved |
| `<OutputType>Library</OutputType>` | `<OutputType>Library</OutputType>` | Preserved or defaulted |
| `<RootNamespace>MyApp</RootNamespace>` | `<RootNamespace>MyApp</RootNamespace>` | Preserved or inferred from filename |
| `<AssemblyName>MyApp</AssemblyName>` | `<AssemblyName>MyApp</AssemblyName>` | Preserved or inferred from filename |
| (none) | `<ProjectGuid>{NEW-GUID}</ProjectGuid>` | **Generated** in Old-style |
| (none) | `<Configuration Condition="'$(Configuration)'==''">Debug</Configuration>` | **Added** |
| (none) | `<Platform Condition="'$(Platform)'==''">AnyCPU</Platform>` | **Added** |
| (none) | `<FileAlignment>512</FileAlignment>` | **Added** |
| (none) | `<Deterministic>true</Deterministic>` | **Added** |
| (none) | Debug PropertyGroup with DebugSymbols, DebugType, Optimize, OutputPath, etc. | **Added** |
| (none) | Release PropertyGroup with DebugType, Optimize, OutputPath, etc. | **Added** |
| `<ImplicitUsings>enable</ImplicitUsings>` | (omitted) | Not supported in Old-style |
| `<Nullable>enable</Nullable>` | (omitted) | Not supported in Old-style |

---

## 4. WinForms-Specific Properties

### Old-style → SDK-style
| Old-style | SDK-style | Notes |
|-----------|-----------|-------|
| (inferred from References) | `<UseWindowsForms>true</UseWindowsForms>` | **Added** if WinForms detected |
| `<Reference Include="System.Windows.Forms" />` | (implicit) | Removed, provided by SDK |
| `<Reference Include="System.Drawing" />` | (implicit) | Removed, provided by SDK |
| (framework version) | (framework version with `-windows` suffix) | **Added** `-windows` for WinForms |

**Detection**: WinForms is detected by:
- References to `System.Windows.Forms` or `System.Drawing`
- Presence of `<UseWindowsForms>true</UseWindowsForms>`
- Presence of `.Designer.cs` files

### SDK-style → Old-style
| SDK-style | Old-style | Notes |
|-----------|-----------|-------|
| `<UseWindowsForms>true</UseWindowsForms>` | (omitted) | Inferred from references |
| (implicit) | `<Reference Include="System.Windows.Forms" />` | **Added** |
| (implicit) | `<Reference Include="System.Drawing" />` | **Added** |

---

## 5. References and Packages

### Old-style → SDK-style
| Old-style | SDK-style | Notes |
|-----------|-----------|-------|
| `<Reference Include="System" />` | (implicit) | Framework references are implicit |
| `<Reference Include="System.Core" />` | (implicit) | |
| `<Reference Include="System.Data" />` | (implicit) | |
| `<Reference Include="System.Xml" />` | (implicit) | |
| `<Reference Include="Microsoft.CSharp" />` | (implicit) | |
| `<Reference Include="ThirdParty"><HintPath>...</HintPath></Reference>` | `<Reference Include="ThirdParty"><HintPath>...</HintPath></Reference>` | Explicit refs preserved |
| packages.config entries | **Manual migration** to `<PackageReference>` | **Not automatic** |

### SDK-style → Old-style
| SDK-style | Old-style | Notes |
|-----------|-----------|-------|
| (implicit framework refs) | `<Reference Include="System" />`, etc. | **Added** standard references |
| `<PackageReference Include="..." />` | ❌ BLOCKS CONVERSION | **Constraint**: No PackageReferences allowed |
| `<Reference Include="ThirdParty"><HintPath>...</HintPath></Reference>` | `<Reference Include="ThirdParty"><HintPath>...</HintPath></Reference>` | Explicit refs preserved |

**Constraint**: SDK→Old-style conversion is **blocked** if any `<PackageReference>` items exist. Old-style uses packages.config, and automatic migration is complex and lossy.

---

## 6. File Items (Compile, Content, etc.)

### Old-style → SDK-style
| Old-style | SDK-style | Notes |
|-----------|-----------|-------|
| `<Compile Include="*.cs" />` | (implicit) | SDK auto-includes *.cs files |
| `<Compile Include="Subdir\*.cs" />` | (implicit) | |
| `<None Include="App.config" />` | (implicit or explicit if needed) | |
| `<EmbeddedResource Include="Form1.resx" />` | (implicit) | SDK auto-includes .resx |
| `<Compile Include="Properties\AssemblyInfo.cs" />` | (omitted) | AssemblyInfo auto-generated in SDK-style |

### SDK-style → Old-style
| SDK-style | Old-style | Notes |
|-----------|-----------|-------|
| (implicit) | `<Compile Include="**\*.cs" />` or explicit items | **NOT ADDED** in basic conversion (would be verbose) |
| (implicit) | `<EmbeddedResource Include="**\*.resx" />` | **NOT ADDED** |

**Note**: Old-style requires explicit `<Compile>` items, but listing every file is verbose and error-prone. The conversion tool **does not** enumerate all files. Users must manually add file items or use wildcards after conversion.

---

## 7. Conversion Constraints and Limitations

### Old-style → SDK-style
- ✅ **Supported**: Single target .NET Framework projects (net40–net48)
- ✅ **Supported**: WinForms, Console, Library projects
- ✅ **Supported**: Variable tokens (e.g., `$(TargetFrameworks)`) preserved verbatim
- ⚠️ **Lossy**: Configuration-specific PropertyGroups (Debug/Release) are omitted in SDK-style
- ⚠️ **Lossy**: Explicit file items (`<Compile>`, etc.) are omitted; SDK auto-includes files
- ⚠️ **Manual**: packages.config must be manually migrated to PackageReference
- ❌ **Not Supported**: WPF projects (would need `<UseWPF>true</UseWPF>`, not auto-detected in this tool)
- ❌ **Not Supported**: Web projects (ASP.NET, etc.)

### SDK-style → Old-style
- ✅ **Supported**: Single .NET Framework target (net40–net48, with or without `-windows`)
- ✅ **Supported**: Variable tokens preserved verbatim
- ❌ **Blocked**: Multi-target projects (`<TargetFrameworks>` with multiple values)
- ❌ **Blocked**: Projects with `<PackageReference>` items
- ❌ **Blocked**: Non-.NET Framework targets (net5.0+, netstandard, netcoreapp)
- ⚠️ **Lossy**: Explicit file items are **not** added; users must add manually
- ⚠️ **Lossy**: SDK-specific properties (e.g., `<ImplicitUsings>`, `<Nullable>`) are dropped

---

## 8. Round-Trip Expectations

### Old→SDK→Old
- ❌ **Not round-trip safe**: Configuration PropertyGroups lost
- ❌ **Not round-trip safe**: Explicit file items lost
- ✅ **Preserves**: Framework version, OutputType, RootNamespace, AssemblyName
- ✅ **Generates**: New ProjectGuid, standard references, Debug/Release PropertyGroups

### SDK→Old→SDK
- ❌ **Not round-trip safe**: Verbose Old-style elements are simplified back to SDK defaults
- ✅ **Preserves**: Framework version (converted back), OutputType, RootNamespace, AssemblyName
- ❌ **Blocked**: If original SDK project had PackageReferences or multi-targeting

---

## 9. Implementation Notes

### Framework Version Conversion Algorithm

**Old-style → SDK-style**:
1. Parse `<TargetFrameworkVersion>` value
2. If starts with `$` → preserve verbatim (variable token)
3. Else, remove `v` prefix and dots: `v4.7.2` → `472` → `net472`
4. If WinForms detected, append `-windows`: `net472` → `net472-windows`

**SDK-style → Old-style**:
1. Parse `<TargetFramework>` value
2. If starts with `$` → preserve verbatim (variable token)
3. Else, remove `net` prefix and `-windows` suffix: `net472-windows` → `472`
4. Add `v` prefix and dots: `472` → `4.7.2` → `v4.7.2`
   - 2 chars: `45` → `v4.5`
   - 3 chars: `472` → `v4.7.2`

### WinForms Detection
- Check for `<Reference Include="System.Windows.Forms" />` or `<Reference Include="System.Drawing" />`
- Check for `<UseWindowsForms>true</UseWindowsForms>`
- Check for `<Compile>` items ending in `.Designer.cs`

### Validation Before SDK→Old-style
1. Ensure `Sdk` attribute is present (is SDK-style)
2. Check for `<PackageReference>` items → **block** if found
3. Check framework is single .NET Framework target (net40–net48) → **block** if not
4. Variables allowed (preserved verbatim)

---

## 10. Test Cases

### Test Case 1: Old-style Console App (net48) → SDK-style
**Input**:
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

**Expected Output**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <OutputType>Exe</OutputType>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>ConsoleApp1</RootNamespace>
    <AssemblyName>ConsoleApp1</AssemblyName>
  </PropertyGroup>
</Project>
```

### Test Case 2: Old-style WinForms App (net472) → SDK-style
**Input**:
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

**Expected Output**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net472-windows</TargetFramework>
    <OutputType>WinExe</OutputType>
    <UseWindowsForms>true</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>WinFormsApp1</RootNamespace>
    <AssemblyName>WinFormsApp1</AssemblyName>
  </PropertyGroup>
</Project>
```

### Test Case 3: SDK-style Console App (net48) → Old-style
**Input**:
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

**Expected Output**:
```xml
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003" ToolsVersion="15.0">
  <Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props" Condition="Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')" />
  <PropertyGroup>
    <Configuration Condition=" '$(Configuration)' == '' ">Debug</Configuration>
    <Platform Condition=" '$(Platform)' == '' ">AnyCPU</Platform>
    <ProjectGuid>{GENERATED-GUID}</ProjectGuid>
    <OutputType>Exe</OutputType>
    <RootNamespace>ConsoleApp1</RootNamespace>
    <AssemblyName>ConsoleApp1</AssemblyName>
    <TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
    <FileAlignment>512</FileAlignment>
    <Deterministic>true</Deterministic>
  </PropertyGroup>
  <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' ">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>bin\Debug\</OutputPath>
    <DefineConstants>DEBUG;TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' ">
    <DebugType>pdbonly</DebugType>
    <Optimize>true</Optimize>
    <OutputPath>bin\Release\</OutputPath>
    <DefineConstants>TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="System" />
    <Reference Include="System.Core" />
    <Reference Include="System.Xml.Linq" />
    <Reference Include="System.Data.DataSetExtensions" />
    <Reference Include="Microsoft.CSharp" />
    <Reference Include="System.Data" />
    <Reference Include="System.Xml" />
  </ItemGroup>
  <Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />
</Project>
```

### Test Case 4: SDK-style with PackageReference → Old-style (BLOCKED)
**Input**:
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

**Expected Result**: ❌ **Conversion blocked** — "Has 1 PackageReference(s)"

### Test Case 5: SDK-style with Multi-Targeting → Old-style (BLOCKED)
**Input**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net48;net6.0</TargetFrameworks>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
</Project>
```

**Expected Result**: ❌ **Conversion blocked** — "Multiple target frameworks"

### Test Case 6: Variable Token Preservation (Old→SDK)
**Input**:
```xml
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003" ToolsVersion="15.0">
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <TargetFrameworkVersion>$(MyCustomFramework)</TargetFrameworkVersion>
  </PropertyGroup>
  <Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />
</Project>
```

**Expected Output**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>$(MyCustomFramework)</TargetFramework>
    <OutputType>Library</OutputType>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

### Test Case 7: Variable Token Preservation (SDK→Old)
**Input**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>$(MyCustomFramework)</TargetFramework>
    <OutputType>Library</OutputType>
  </PropertyGroup>
</Project>
```

**Expected Output**:
```xml
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003" ToolsVersion="15.0">
  <Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props" Condition="Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')" />
  <PropertyGroup>
    <Configuration Condition=" '$(Configuration)' == '' ">Debug</Configuration>
    <Platform Condition=" '$(Platform)' == '' ">AnyCPU</Platform>
    <ProjectGuid>{GENERATED-GUID}</ProjectGuid>
    <OutputType>Library</OutputType>
    <RootNamespace>ProjectName</RootNamespace>
    <AssemblyName>ProjectName</AssemblyName>
    <TargetFrameworkVersion>$(MyCustomFramework)</TargetFrameworkVersion>
    <FileAlignment>512</FileAlignment>
    <Deterministic>true</Deterministic>
  </PropertyGroup>
  <!-- Debug and Release PropertyGroups -->
  <ItemGroup>
    <Reference Include="System" />
    <Reference Include="System.Core" />
    <Reference Include="System.Xml.Linq" />
    <Reference Include="System.Data.DataSetExtensions" />
    <Reference Include="Microsoft.CSharp" />
    <Reference Include="System.Data" />
    <Reference Include="System.Xml" />
  </ItemGroup>
  <Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />
</Project>
```

---

## 11. Summary

This reference document defines the mappings and constraints for converting C# projects between Old-style and SDK-style formats. The conversion is **lossy** and **not round-trip safe**, but preserves essential properties like framework version, output type, and namespace/assembly names. 

**Key Takeaways**:
- Old→SDK: Simplifies and removes verbose elements; implicit defaults
- SDK→Old: Expands to verbose format; blocked if PackageReferences or multi-targeting
- Variables: Always preserved verbatim
- WinForms: Auto-detected and handled with `-windows` suffix and `<UseWindowsForms>`
- Files: Not enumerated; SDK auto-includes, Old-style requires manual addition
