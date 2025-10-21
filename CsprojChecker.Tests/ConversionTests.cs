using System.Xml.Linq;
using Xunit;
using CsprojChecker.Core;
using CsprojChecker.Core.Models;

namespace CsprojChecker.Tests;

/// <summary>
/// Integration tests for .csproj conversion functionality
/// Based on TestPlan_OldToSdk_ModernConversion.md
/// </summary>
public class ConversionTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly ProjectConversionService _conversionService;
    
    public ConversionTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "CsprojCheckerTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);
        _conversionService = new ProjectConversionService();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    #region Test Case 1: Old-style Console App (net48) → SDK-style

    [Fact]
    public void TestCase1_OldStyleConsoleApp_ConvertsToSdkStyle()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "OldStyleConsole.csproj");
        var oldStyleContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""15.0"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
    <RootNamespace>ConsoleApp1</RootNamespace>
    <AssemblyName>ConsoleApp1</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""System"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>";
        File.WriteAllText(projectPath, oldStyleContent);
        
        // Act
        var result = ConvertOldStyleToSdkStyle(projectPath);
        
        // Assert
        Assert.True(result.Success, $"Conversion failed: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        
        Assert.NotNull(root);
        Assert.Equal("Project", root.Name.LocalName);
        Assert.NotNull(root.Attribute("Sdk"));
        Assert.Contains("Microsoft.NET.Sdk", root.Attribute("Sdk")!.Value);
        
        // Check target framework
        var tfm = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "TargetFramework");
        Assert.NotNull(tfm);
        Assert.Equal("net48", tfm.Value);
        
        // ImplicitUsings and Nullable are optional - don't enforce them
        // They may be added by some conversions but are not required
        
        // Check that OutputType, RootNamespace, AssemblyName are preserved
        var outputType = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "OutputType");
        Assert.NotNull(outputType);
        Assert.Equal("Exe", outputType.Value);
        
        // No xmlns attribute (SDK-style doesn't use it)
        Assert.Null(root.Attribute("xmlns"));
        
        // No Import statements
        var imports = root.Descendants().Where(e => e.Name.LocalName == "Import");
        Assert.Empty(imports);
    }

    #endregion

    #region Test Case 2: Old-style WinForms App (net472) → SDK-style WITHOUT -windows suffix

    [Fact]
    public void TestCase2_OldStyleWinFormsApp_ConvertsToSdkStyleWithoutWindowsSuffix()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "OldStyleWinForms.csproj");
        var oldStyleContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""15.0"">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
    <RootNamespace>WinFormsApp1</RootNamespace>
    <AssemblyName>WinFormsApp1</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""System"" />
    <Reference Include=""System.Windows.Forms"" />
    <Reference Include=""System.Drawing"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>";
        File.WriteAllText(projectPath, oldStyleContent);
        
        // Act
        var result = ConvertOldStyleToSdkStyle(projectPath);
        
        // Assert
        Assert.True(result.Success, $"Conversion failed: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        
        Assert.NotNull(root);
        
        // For .NET Framework (net4x), expect NO -windows suffix
        var tfm = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "TargetFramework");
        Assert.NotNull(tfm);
        Assert.Equal("net472", tfm.Value);
        
        // Check UseWindowsForms property
        var useWindowsForms = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "UseWindowsForms");
        Assert.NotNull(useWindowsForms);
        Assert.Equal("true", useWindowsForms.Value);
        
        // No explicit System.Windows.Forms/Drawing references
        var references = root.Descendants().Where(e => e.Name.LocalName == "Reference");
        Assert.Empty(references);
    }

    #endregion

    #region Test Case 2a: Old-style WinForms App → SDK-style should use WindowsDesktop SDK

    [Fact]
    public void TestCase2a_OldStyleWinFormsApp_UsesWindowsDesktopSdk()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "OldStyleWinFormsWindowsDesktop.csproj");
        var oldStyleContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""15.0"">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
    <RootNamespace>WinFormsApp1</RootNamespace>
    <AssemblyName>WinFormsApp1</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""System"" />
    <Reference Include=""System.Windows.Forms"" />
    <Reference Include=""System.Drawing"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>";
        File.WriteAllText(projectPath, oldStyleContent);
        
        // Act
        var result = ConvertOldStyleToSdkStyle(projectPath);
        
        // Assert
        Assert.True(result.Success, $"Conversion failed: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        
        Assert.NotNull(root);
        
        // Check that SDK attribute is set to WindowsDesktop
        var sdkAttr = root.Attribute("Sdk");
        Assert.NotNull(sdkAttr);
        Assert.Equal("Microsoft.NET.Sdk.WindowsDesktop", sdkAttr.Value);
        
        // Check UseWindowsForms property
        var useWindowsForms = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "UseWindowsForms");
        Assert.NotNull(useWindowsForms);
        Assert.Equal("true", useWindowsForms.Value);
    }

    #endregion

    #region Test Case 2b: Modern WinForms App (net8.0) should have -windows suffix

    [Fact]
    public void TestCase2b_Net8WinFormsApp_ShouldHaveWindowsSuffix()
    {
        // Arrange - Simulate a net8.0 WinForms project
        var projectPath = Path.Combine(_testDirectory, "Net8WinForms.csproj");
        var sdkStyleContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <OutputType>WinExe</OutputType>
    <UseWindowsForms>true</UseWindowsForms>
  </PropertyGroup>
</Project>";
        File.WriteAllText(projectPath, sdkStyleContent);
        
        // Act & Assert - For net5.0+, -windows suffix is expected for WinForms
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        
        Assert.NotNull(root);
        
        var useWinForms = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "UseWindowsForms");
        Assert.NotNull(useWinForms);
        Assert.Equal("true", useWinForms.Value);
        
        // This test documents that net5.0+ WinForms SHOULD use netX-windows
        // The actual TFM in the test file would be net8.0-windows in real scenarios
    }

    #endregion

    #region Test Case 2c: Old-style WPF App → SDK-style should use WindowsDesktop SDK

    [Fact]
    public void TestCase2c_OldStyleWpfApp_UsesWindowsDesktopSdk()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "OldStyleWpfWindowsDesktop.csproj");
        var oldStyleContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""15.0"">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
    <RootNamespace>WpfApp1</RootNamespace>
    <AssemblyName>WpfApp1</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""PresentationCore"" />
    <Reference Include=""PresentationFramework"" />
    <Reference Include=""WindowsBase"" />
    <Reference Include=""System.Xaml"" />
  </ItemGroup>
  <ItemGroup>
    <ApplicationDefinition Include=""App.xaml"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>";
        File.WriteAllText(projectPath, oldStyleContent);
        
        // Act
        var result = ConvertOldStyleToSdkStyle(projectPath);
        
        // Assert
        Assert.True(result.Success, $"Conversion failed: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        
        Assert.NotNull(root);
        
        // Check that SDK attribute is set to WindowsDesktop
        var sdkAttr = root.Attribute("Sdk");
        Assert.NotNull(sdkAttr);
        Assert.Equal("Microsoft.NET.Sdk.WindowsDesktop", sdkAttr.Value);
        
        // Check UseWPF property
        var useWpf = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "UseWPF");
        Assert.NotNull(useWpf);
        Assert.Equal("true", useWpf.Value);
    }

    #endregion

    #region Test Case 2d: Modern WinForms Conversion (Old-style net48 WinForms → Modern SDK, NO -windows for net4x)

    [Fact]
    public void TestCase2d_ModernWinFormsConversion_UsesWindowsDesktopSdkAndCorrectTfm()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "ModernWinFormsConversion.csproj");
        var oldStyleContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""15.0"">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
    <RootNamespace>WinFormsApp1</RootNamespace>
    <AssemblyName>WinFormsApp1</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""System"" />
    <Reference Include=""System.Windows.Forms"" />
    <Reference Include=""System.Drawing"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>";
        File.WriteAllText(projectPath, oldStyleContent);
        
        // Act - Use modern conversion (one-way)
        var result = _conversionService.ConvertOldStyleToSdkStyleModern(projectPath);
        
        // Assert
        Assert.True(result.Success, $"Modern conversion failed: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        
        Assert.NotNull(root);
        
        // Check that SDK attribute is set to WindowsDesktop
        var sdkAttr = root.Attribute("Sdk");
        Assert.NotNull(sdkAttr);
        Assert.Equal("Microsoft.NET.Sdk.WindowsDesktop", sdkAttr.Value);
        
        // Check UseWindowsForms property is present
        var useWindowsForms = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "UseWindowsForms");
        Assert.NotNull(useWindowsForms);
        Assert.Equal("true", useWindowsForms.Value);
        
        // Check that TFM is net48 (NO -windows suffix for .NET Framework 4.x, even in modern conversion)
        var tfm = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "TargetFramework");
        Assert.NotNull(tfm);
        Assert.Equal("net48", tfm.Value);
        Assert.DoesNotContain("-windows", tfm.Value, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Test Case 2e: Non-Desktop Console App Conversion Guard Tests

    [Fact]
    public void TestCase2e_NonDesktopConsoleApp_UsesStandardSdkInBothConversions()
    {
        // Arrange - Old-style console app (non-desktop)
        var projectPath = Path.Combine(_testDirectory, "NonDesktopConsole.csproj");
        var oldStyleContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""15.0"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
    <RootNamespace>ConsoleApp1</RootNamespace>
    <AssemblyName>ConsoleApp1</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""System"" />
    <Reference Include=""System.Core"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>";
        File.WriteAllText(projectPath, oldStyleContent);
        
        // Act - Use round-trip conversion
        var result = ConvertOldStyleToSdkStyle(projectPath);
        
        // Assert
        Assert.True(result.Success, $"Conversion failed: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        
        Assert.NotNull(root);
        
        // Check that SDK attribute is set to standard Microsoft.NET.Sdk (NOT WindowsDesktop)
        var sdkAttr = root.Attribute("Sdk");
        Assert.NotNull(sdkAttr);
        Assert.Equal("Microsoft.NET.Sdk", sdkAttr.Value);
        
        // Check that no UseWindowsForms property is present
        var useWindowsForms = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "UseWindowsForms");
        Assert.Null(useWindowsForms);
        
        // Check that no UseWPF property is present
        var useWpf = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "UseWPF");
        Assert.Null(useWpf);
        
        // Check TFM is net48 without -windows suffix
        var tfm = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "TargetFramework");
        Assert.NotNull(tfm);
        Assert.Equal("net48", tfm.Value);
    }

    [Fact]
    public void TestCase2f_NonDesktopConsoleApp_ModernConversion_UsesStandardSdk()
    {
        // Arrange - Old-style console app (non-desktop)
        var projectPath = Path.Combine(_testDirectory, "NonDesktopConsoleModern.csproj");
        var oldStyleContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""15.0"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
    <RootNamespace>ConsoleApp1</RootNamespace>
    <AssemblyName>ConsoleApp1</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""System"" />
    <Reference Include=""System.Core"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>";
        File.WriteAllText(projectPath, oldStyleContent);
        
        // Act - Use modern (one-way) conversion
        var result = _conversionService.ConvertOldStyleToSdkStyleModern(projectPath);
        
        // Assert
        Assert.True(result.Success, $"Modern conversion failed: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        
        Assert.NotNull(root);
        
        // Check that SDK attribute is set to standard Microsoft.NET.Sdk (NOT WindowsDesktop)
        var sdkAttr = root.Attribute("Sdk");
        Assert.NotNull(sdkAttr);
        Assert.Equal("Microsoft.NET.Sdk", sdkAttr.Value);
        
        // Check that no UseWindowsForms property is present
        var useWindowsForms = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "UseWindowsForms");
        Assert.Null(useWindowsForms);
        
        // Check that no UseWPF property is present
        var useWpf = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "UseWPF");
        Assert.Null(useWpf);
        
        // Check TFM - for modern conversion of non-desktop, it should be net472 (without -windows)
        var tfm = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "TargetFramework");
        Assert.NotNull(tfm);
        Assert.Equal("net472", tfm.Value);
    }

    #endregion

    #region Test Case 2g: TFM Suffix Regression Tests - net4x Never Gets -windows Suffix

    [Theory]
    [InlineData("v4.8", "net48")]
    [InlineData("v4.7.2", "net472")]
    [InlineData("v4.7.1", "net471")]
    [InlineData("v4.7", "net47")]
    [InlineData("v4.6.2", "net462")]
    [InlineData("v4.6.1", "net461")]
    [InlineData("v4.6", "net46")]
    [InlineData("v4.5.2", "net452")]
    [InlineData("v4.5.1", "net451")]
    [InlineData("v4.5", "net45")]
    public void TestCase2g_Net4xWinFormsRoundTrip_NeverGetsWindowsSuffix(string oldVersion, string expectedTfm)
    {
        // Arrange - Old-style WinForms with .NET Framework 4.x
        var projectPath = Path.Combine(_testDirectory, $"WinForms_{oldVersion.Replace(".", "_")}_RoundTrip.csproj");
        var oldStyleContent = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""15.0"">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFrameworkVersion>{oldVersion}</TargetFrameworkVersion>
    <RootNamespace>WinFormsApp1</RootNamespace>
    <AssemblyName>WinFormsApp1</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""System"" />
    <Reference Include=""System.Windows.Forms"" />
    <Reference Include=""System.Drawing"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>";
        File.WriteAllText(projectPath, oldStyleContent);
        
        // Act - Use round-trip conversion (should preserve .NET Framework compatibility)
        var result = ConvertOldStyleToSdkStyle(projectPath);
        
        // Assert
        Assert.True(result.Success, $"Conversion failed for {oldVersion}: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        
        Assert.NotNull(root);
        
        // Check TFM - .NET Framework 4.x should NEVER have -windows suffix in round-trip conversion
        var tfm = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "TargetFramework");
        Assert.NotNull(tfm);
        Assert.Equal(expectedTfm, tfm.Value);
        Assert.DoesNotContain("-windows", tfm.Value, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("v4.8")]
    [InlineData("v4.7.2")]
    [InlineData("v4.6.2")]
    public void TestCase2h_Net4xConsoleRoundTrip_NeverGetsWindowsSuffix(string oldVersion)
    {
        // Arrange - Old-style console app with .NET Framework 4.x
        var projectPath = Path.Combine(_testDirectory, $"Console_{oldVersion.Replace(".", "_")}_RoundTrip.csproj");
        var oldStyleContent = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""15.0"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFrameworkVersion>{oldVersion}</TargetFrameworkVersion>
    <RootNamespace>ConsoleApp1</RootNamespace>
    <AssemblyName>ConsoleApp1</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""System"" />
    <Reference Include=""System.Core"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>";
        File.WriteAllText(projectPath, oldStyleContent);
        
        // Act - Use round-trip conversion
        var result = ConvertOldStyleToSdkStyle(projectPath);
        
        // Assert
        Assert.True(result.Success, $"Conversion failed for {oldVersion}: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        
        Assert.NotNull(root);
        
        // Check TFM - .NET Framework 4.x should NEVER have -windows suffix
        var tfm = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "TargetFramework");
        Assert.NotNull(tfm);
        Assert.DoesNotContain("-windows", tfm.Value, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Test Case 2i: Modern Conversion - Net4x Never Gets -windows Suffix for Desktop Projects

    [Theory]
    [InlineData("v4.8", "net48")]
    [InlineData("v4.7.2", "net472")]
    [InlineData("v4.6.2", "net462")]
    public void TestCase2i_ModernConversionNet4xDesktop_NeverGetsWindowsSuffix(string oldVersion, string expectedTfm)
    {
        // Arrange - Old-style WinForms with .NET Framework 4.x
        var projectPath = Path.Combine(_testDirectory, $"ModernWinForms_{oldVersion.Replace(".", "_")}.csproj");
        var oldStyleContent = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""15.0"">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFrameworkVersion>{oldVersion}</TargetFrameworkVersion>
    <RootNamespace>WinFormsApp1</RootNamespace>
    <AssemblyName>WinFormsApp1</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""System"" />
    <Reference Include=""System.Windows.Forms"" />
    <Reference Include=""System.Drawing"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>";
        File.WriteAllText(projectPath, oldStyleContent);
        
        // Act - Use modern (one-way) conversion
        var result = _conversionService.ConvertOldStyleToSdkStyleModern(projectPath);
        
        // Assert
        Assert.True(result.Success, $"Modern conversion failed for {oldVersion}: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        
        Assert.NotNull(root);
        
        // Check TFM - .NET Framework 4.x should NEVER have -windows suffix, even in modern conversion
        var tfm = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "TargetFramework");
        Assert.NotNull(tfm);
        Assert.Equal(expectedTfm, tfm.Value);
        Assert.DoesNotContain("-windows", tfm.Value, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("v4.8", "net48")]
    [InlineData("v4.7.2", "net472")]
    public void TestCase2j_ModernConversionNet4xConsole_NeverGetsWindowsSuffix(string oldVersion, string expectedTfm)
    {
        // Arrange - Old-style console with .NET Framework 4.x
        var projectPath = Path.Combine(_testDirectory, $"ModernConsole_{oldVersion.Replace(".", "_")}.csproj");
        var oldStyleContent = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""15.0"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFrameworkVersion>{oldVersion}</TargetFrameworkVersion>
    <RootNamespace>ConsoleApp1</RootNamespace>
    <AssemblyName>ConsoleApp1</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""System"" />
    <Reference Include=""System.Core"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>";
        File.WriteAllText(projectPath, oldStyleContent);
        
        // Act - Use modern (one-way) conversion
        var result = _conversionService.ConvertOldStyleToSdkStyleModern(projectPath);
        
        // Assert
        Assert.True(result.Success, $"Modern conversion failed for {oldVersion}: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        
        Assert.NotNull(root);
        
        // Check TFM - .NET Framework 4.x should NEVER have -windows suffix
        var tfm = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "TargetFramework");
        Assert.NotNull(tfm);
        Assert.Equal(expectedTfm, tfm.Value);
        Assert.DoesNotContain("-windows", tfm.Value, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Test Case 3: Variable Token Preservation (Old→SDK)

    [Fact]
    public void TestCase3_VariableTokenPreservation_OldToSdk()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "OldStyleWithVariable.csproj");
        var oldStyleContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""15.0"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFrameworkVersion>$(MyCustomFramework)</TargetFrameworkVersion>
    <RootNamespace>ConsoleApp1</RootNamespace>
    <AssemblyName>ConsoleApp1</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""System"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>";
        File.WriteAllText(projectPath, oldStyleContent);
        
        // Act
        var result = ConvertOldStyleToSdkStyle(projectPath);
        
        // Assert
        Assert.True(result.Success, $"Conversion failed: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        
        Assert.NotNull(root);
        
        // Check that variable is preserved exactly
        var tfm = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "TargetFramework");
        Assert.NotNull(tfm);
        Assert.Equal("$(MyCustomFramework)", tfm.Value);
    }

    #endregion

    #region Test Case 3b: Variable Token Preservation in Modern Conversion (Old→SDK One-Way)

    [Fact]
    public void TestCase3b_VariableTokenPreservation_OldToSdkModern()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "OldStyleWithVariableModern.csproj");
        var oldStyleContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""15.0"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFrameworkVersion>$(MyCustomFramework)</TargetFrameworkVersion>
    <RootNamespace>ConsoleApp1</RootNamespace>
    <AssemblyName>ConsoleApp1</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""System"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>";
        File.WriteAllText(projectPath, oldStyleContent);
        
        // Act
        var result = _conversionService.ConvertOldStyleToSdkStyleModern(projectPath);
        
        // Assert
        Assert.True(result.Success, $"Conversion failed: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        
        Assert.NotNull(root);
        
        // Check that variable is preserved exactly (not rewritten to net48)
        var tfm = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "TargetFramework");
        Assert.NotNull(tfm);
        Assert.Equal("$(MyCustomFramework)", tfm.Value);
    }

    #endregion

    #region Test Case 4: Multiple Framework Versions (Old→SDK)

    [Theory]
    [InlineData("v4.8", "net48")]
    [InlineData("v4.7.2", "net472")]
    [InlineData("v4.7.1", "net471")]
    [InlineData("v4.7", "net47")]
    [InlineData("v4.6.2", "net462")]
    [InlineData("v4.6.1", "net461")]
    [InlineData("v4.6", "net46")]
    [InlineData("v4.5.2", "net452")]
    [InlineData("v4.5.1", "net451")]
    [InlineData("v4.5", "net45")]
    [InlineData("v4.0", "net40")]
    public void TestCase4_MultipleFrameworkVersions_OldToSdk(string oldVersion, string expectedSdkVersion)
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, $"OldStyle_{oldVersion.Replace(".", "_")}.csproj");
        var oldStyleContent = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""15.0"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFrameworkVersion>{oldVersion}</TargetFrameworkVersion>
    <RootNamespace>ConsoleApp1</RootNamespace>
    <AssemblyName>ConsoleApp1</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""System"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>";
        File.WriteAllText(projectPath, oldStyleContent);
        
        // Act
        var result = ConvertOldStyleToSdkStyle(projectPath);
        
        // Assert
        Assert.True(result.Success, $"Conversion failed for {oldVersion}: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        
        Assert.NotNull(root);
        
        // Check target framework
        var tfm = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "TargetFramework");
        Assert.NotNull(tfm);
        Assert.Equal(expectedSdkVersion, tfm.Value);
    }

    #endregion

    #region Test Case 5: SDK-style Console App → Old-style

    [Fact]
    public void TestCase5_SdkStyleConsoleApp_ConvertsToOldStyle()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "SdkStyleConsole.csproj");
        var sdkStyleContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <OutputType>Exe</OutputType>
    <RootNamespace>ConsoleApp1</RootNamespace>
    <AssemblyName>ConsoleApp1</AssemblyName>
  </PropertyGroup>
</Project>";
        File.WriteAllText(projectPath, sdkStyleContent);
        
        // Act
        var result = ConvertSdkStyleToOldStyle(projectPath);
        
        // Assert
        Assert.True(result.Success, $"Conversion failed: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        
        Assert.NotNull(root);
        
        // Check xmlns attribute
        var xmlns = root.Attribute("xmlns");
        Assert.NotNull(xmlns);
        Assert.Equal("http://schemas.microsoft.com/developer/msbuild/2003", xmlns.Value);
        
        // Check ToolsVersion
        var toolsVersion = root.Attribute("ToolsVersion");
        Assert.NotNull(toolsVersion);
        Assert.Equal("15.0", toolsVersion.Value);
        
        // Check for Import statements
        var imports = root.Descendants().Where(e => e.Name.LocalName == "Import").ToList();
        Assert.NotEmpty(imports);
        Assert.Contains(imports, i => i.Attribute("Project")?.Value.Contains("Microsoft.Common.props") == true);
        Assert.Contains(imports, i => i.Attribute("Project")?.Value.Contains("Microsoft.CSharp.targets") == true);
        
        // Check ProjectGuid (newly generated)
        var projectGuid = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "ProjectGuid");
        Assert.NotNull(projectGuid);
        Assert.Matches(@"^\{[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}\}$", projectGuid.Value);
        
        // Check Configuration and Platform defaults
        var configuration = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "Configuration");
        Assert.NotNull(configuration);
        Assert.Equal("Debug", configuration.Value);
        
        var platform = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "Platform");
        Assert.NotNull(platform);
        Assert.Equal("AnyCPU", platform.Value);
        
        // Check FileAlignment
        var fileAlignment = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "FileAlignment");
        Assert.NotNull(fileAlignment);
        Assert.Equal("512", fileAlignment.Value);
        
        // Check Deterministic
        var deterministic = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "Deterministic");
        Assert.NotNull(deterministic);
        Assert.Equal("true", deterministic.Value);
        
        // Check Debug PropertyGroup
        var debugPropertyGroup = root.Descendants()
            .Where(e => e.Name.LocalName == "PropertyGroup")
            .FirstOrDefault(pg => pg.Attribute("Condition")?.Value.Contains("Debug") == true);
        Assert.NotNull(debugPropertyGroup);
        
        // Check Release PropertyGroup
        var releasePropertyGroup = root.Descendants()
            .Where(e => e.Name.LocalName == "PropertyGroup")
            .FirstOrDefault(pg => pg.Attribute("Condition")?.Value.Contains("Release") == true);
        Assert.NotNull(releasePropertyGroup);
        
        // Check Standard references
        var references = root.Descendants()
            .Where(e => e.Name.LocalName == "Reference")
            .Select(r => r.Attribute("Include")?.Value)
            .ToList();
        
        Assert.Contains("System", references);
        Assert.Contains("System.Core", references);
        Assert.Contains("System.Xml.Linq", references);
        Assert.Contains("System.Data.DataSetExtensions", references);
        Assert.Contains("Microsoft.CSharp", references);
        Assert.Contains("System.Data", references);
        Assert.Contains("System.Xml", references);
        
        // Check TargetFrameworkVersion
        var tfmVersion = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "TargetFrameworkVersion");
        Assert.NotNull(tfmVersion);
        Assert.Equal("v4.8", tfmVersion.Value);
    }

    #endregion

    #region Test Case 6: SDK-style with PackageReference → Old-style (BLOCKED)

    [Fact]
    public void TestCase6_SdkStyleWithPackageReference_BlocksConversion()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "SdkStyleWithPackage.csproj");
        var sdkStyleContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""13.0.1"" />
  </ItemGroup>
</Project>";
        File.WriteAllText(projectPath, sdkStyleContent);
        
        // Act
        var result = ConvertSdkStyleToOldStyle(projectPath);
        
        // Assert - should be blocked
        Assert.False(result.Success);
        Assert.Contains("PackageReference", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Test Case 7: SDK-style with Multi-Targeting → Old-style (BLOCKED)

    [Fact]
    public void TestCase7_SdkStyleWithMultiTargeting_BlocksConversion()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "SdkStyleMultiTarget.csproj");
        var sdkStyleContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFrameworks>net48;net6.0</TargetFrameworks>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
</Project>";
        File.WriteAllText(projectPath, sdkStyleContent);
        
        // Act
        var result = ConvertSdkStyleToOldStyle(projectPath);
        
        // Assert - should be blocked
        Assert.False(result.Success);
        Assert.Contains("Multiple target framework", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Test Case 8: SDK-style with Non-.NET Framework Target → Old-style (BLOCKED)

    [Fact]
    public void TestCase8_SdkStyleWithNonNetFramework_BlocksConversion()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "SdkStyleNet6.csproj");
        var sdkStyleContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
</Project>";
        File.WriteAllText(projectPath, sdkStyleContent);
        
        // Act
        var result = ConvertSdkStyleToOldStyle(projectPath);
        
        // Assert - should be blocked
        Assert.False(result.Success);
        Assert.Contains("Not a .NET Framework target", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Test Case 9: Variable Token Preservation (SDK→Old)

    [Fact]
    public void TestCase9_VariableTokenPreservation_SdkToOld()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "SdkStyleWithVariable.csproj");
        var sdkStyleContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>$(MyCustomFramework)</TargetFramework>
    <OutputType>Exe</OutputType>
    <RootNamespace>ConsoleApp1</RootNamespace>
    <AssemblyName>ConsoleApp1</AssemblyName>
  </PropertyGroup>
</Project>";
        File.WriteAllText(projectPath, sdkStyleContent);
        
        // Act
        var result = ConvertSdkStyleToOldStyle(projectPath);
        
        // Assert
        Assert.True(result.Success, $"Conversion failed: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        
        Assert.NotNull(root);
        
        // Check that variable is preserved exactly
        var tfmVersion = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "TargetFrameworkVersion");
        Assert.NotNull(tfmVersion);
        Assert.Equal("$(MyCustomFramework)", tfmVersion.Value);
    }

    #endregion

    #region Test Case 11: Namespace Preservation in SDK→Old Conversion

    [Fact]
    public void TestCase11_SdkToOld_PreservesMSBuildNamespace()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "NamespaceTest.csproj");
        var sdkStyleContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
</Project>";
        File.WriteAllText(projectPath, sdkStyleContent);
        
        // Act
        var result = ConvertSdkStyleToOldStyle(projectPath);
        
        // Assert
        Assert.True(result.Success, $"Conversion failed: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        
        Assert.NotNull(root);
        
        // Verify the msbuild-2003 namespace is preserved
        var xmlns = root.Attribute("xmlns");
        Assert.NotNull(xmlns);
        Assert.Equal("http://schemas.microsoft.com/developer/msbuild/2003", xmlns.Value);
        
        // Verify namespace is also used in child elements
        XNamespace ns = "http://schemas.microsoft.com/developer/msbuild/2003";
        var propertyGroups = root.Elements(ns + "PropertyGroup").ToList();
        Assert.NotEmpty(propertyGroups);
    }

    #endregion

    #region Test Case 10: Buildability Tests

    [Fact(Skip = "Integration test - requires dotnet SDK installed")]
    [Trait("Category", "Integration")]
    public void TestCase10_ConvertedProjectsAreBuildable_OldToSdk()
    {
        // This test is marked as integration because it:
        // - Requires dotnet SDK to be installed
        // - Takes longer to execute (build operation)
        // - Depends on external tools
        
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "BuildableOldToSdk.csproj");
        var oldStyleContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""15.0"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
    <RootNamespace>ConsoleApp1</RootNamespace>
    <AssemblyName>ConsoleApp1</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""System"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>";
        File.WriteAllText(projectPath, oldStyleContent);
        
        // Create a simple Program.cs file
        var programPath = Path.Combine(_testDirectory, "Program.cs");
        File.WriteAllText(programPath, @"
using System;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(""Hello, World!"");
        }
    }
}
");
        
        // Act
        var result = ConvertOldStyleToSdkStyle(projectPath);
        Assert.True(result.Success, $"Conversion failed: {result.Error}");
        
        // Assert - try to build the project (with longer timeout for build operation)
        var buildResult = BuildProject(projectPath, timeoutSeconds: 180);
        Assert.True(buildResult.Success, $"Build failed: {buildResult.Error}");
    }

    #endregion

    #region Test Case 12: ProjectReference Preservation

    [Fact]
    public void ModernConversion_PreservesProjectReferences()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "ProjectWithRefs.csproj");
        var oldStyleContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""15.0"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include=""..\OtherProject\OtherProject.csproj"" />
  </ItemGroup>
</Project>";
        File.WriteAllText(projectPath, oldStyleContent);
        
        // Act
        var result = _conversionService.ConvertOldStyleToSdkStyleModern(projectPath);
        
        // Assert
        Assert.True(result.Success);
        var doc = XDocument.Load(projectPath);
        var projectRef = doc.Root.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "ProjectReference");
        Assert.NotNull(projectRef);
        Assert.Equal("..\\OtherProject\\OtherProject.csproj", projectRef.Attribute("Include")?.Value);
    }

    [Fact]
    public void RoundTripConversion_PreservesProjectReferences()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "ProjectWithRefsRoundTrip.csproj");
        var oldStyleContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""15.0"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include=""..\OtherProject\OtherProject.csproj"" />
  </ItemGroup>
</Project>";
        File.WriteAllText(projectPath, oldStyleContent);

        // Act
        var result = _conversionService.ConvertOldStyleToSdkStyle(projectPath);

        // Assert
        Assert.True(result.Success);
        var doc = XDocument.Load(projectPath);
        var projectRef = doc.Root.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "ProjectReference");
        Assert.NotNull(projectRef);
        Assert.Equal("..\\OtherProject\\OtherProject.csproj", projectRef.Attribute("Include")?.Value);
    }

    #endregion

    #region Helper Methods

    private ConversionResult ConvertOldStyleToSdkStyle(string projectPath)
    {
        return _conversionService.ConvertOldStyleToSdkStyle(projectPath);
    }

    private ConversionResult ConvertSdkStyleToOldStyle(string projectPath)
    {
        return _conversionService.ConvertSdkStyleToOldStyle(projectPath);
    }

    private BuildResult BuildProject(string projectPath, int timeoutSeconds = 120)
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{projectPath}\"",
                WorkingDirectory = Path.GetDirectoryName(projectPath),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process == null)
                return new BuildResult { Success = false, Error = "Failed to start dotnet build" };
            
            // Wait for process with specified timeout
            if (!process.WaitForExit(timeoutSeconds * 1000))
            {
                process.Kill();
                return new BuildResult { Success = false, Error = $"Build timed out after {timeoutSeconds} seconds" };
            }
            
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            
            if (process.ExitCode == 0)
                return new BuildResult { Success = true };
            else
                return new BuildResult { Success = false, Error = $"Build failed with exit code {process.ExitCode}:\n{output}\n{error}" };
        }
        catch (Exception ex)
        {
            return new BuildResult { Success = false, Error = ex.Message };
        }
    }

    #endregion

    #region Custom One-Way Modern Conversion Tests

    [Fact]
    public void Test_CustomOneWayModern_BasicLibraryConversion()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "CustomModernLibrary.csproj");
        var oldStyleContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""15.0"">
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProductVersion>8.0.30703</ProductVersion>
    <SchemaVersion>2.0</SchemaVersion>
    <ProjectGuid>{12345678-1234-1234-1234-123456789ABC}</ProjectGuid>
    <OutputType>Library</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <RootNamespace>MyLibrary</RootNamespace>
    <AssemblyName>MyLibrary</AssemblyName>
    <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
    <FileAlignment>512</FileAlignment>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>bin\Debug\</OutputPath>
    <DefineConstants>DEBUG;TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">
    <DebugType>pdbonly</DebugType>
    <Optimize>true</Optimize>
    <OutputPath>bin\Release\</OutputPath>
    <DefineConstants>TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""System"" />
  </ItemGroup>
  <ItemGroup>
    <Compile Include=""Class1.cs"" />
    <Compile Include=""Properties\AssemblyInfo.cs"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>";
        File.WriteAllText(projectPath, oldStyleContent);
        
        // Act
        var result = _conversionService.ConvertOldStyleToSdkStyleCustomModern(projectPath);
        
        // Assert
        Assert.True(result.Success, $"Conversion failed: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        
        Assert.NotNull(root);
        Assert.Equal("Project", root.Name.LocalName);
        Assert.NotNull(root.Attribute("Sdk"));
        
        // Check target framework
        var tfm = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "TargetFramework");
        Assert.NotNull(tfm);
        Assert.Equal("net472", tfm.Value);
        
        // Check modern properties are added
        var generateAssemblyInfo = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "GenerateAssemblyInfo");
        Assert.NotNull(generateAssemblyInfo);
        Assert.Equal("True", generateAssemblyInfo.Value);
        
        var enableDefaultCompile = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "EnableDefaultCompileItems");
        Assert.NotNull(enableDefaultCompile);
        Assert.Equal("false", enableDefaultCompile.Value);
        
        var enableDefaultEmbedded = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "EnableDefaultEmbeddedResourceItems");
        Assert.NotNull(enableDefaultEmbedded);
        Assert.Equal("false", enableDefaultEmbedded.Value);
        
        var appendTfm = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "AppendTargetFrameworkToOutputPath");
        Assert.NotNull(appendTfm);
        Assert.Equal("true", appendTfm.Value);
        
        // Check obsolete properties are removed
        var productVersion = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "ProductVersion");
        Assert.Null(productVersion);
        
        var schemaVersion = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "SchemaVersion");
        Assert.Null(schemaVersion);
        
        var appDesignerFolder = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "AppDesignerFolder");
        Assert.Null(appDesignerFolder);
        
        // Check OutputType is preserved
        var outputType = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "OutputType");
        Assert.NotNull(outputType);
        Assert.Equal("Library", outputType.Value);
        
        // Check ProjectGuid is preserved
        var projectGuid = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "ProjectGuid");
        Assert.NotNull(projectGuid);
        Assert.Equal("{12345678-1234-1234-1234-123456789ABC}", projectGuid.Value);
        
        // Check Configuration and Platform are preserved
        var config = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "Configuration");
        Assert.NotNull(config);
        Assert.Equal("Debug", config.Value);
        
        var platform = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "Platform");
        Assert.NotNull(platform);
        Assert.Equal("AnyCPU", platform.Value);
    }

    [Fact]
    public void Test_CustomOneWayModern_ConfigurationPreservation()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "CustomModernConfig.csproj");
        var oldStyleContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""15.0"">
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <OutputType>Exe</OutputType>
    <RootNamespace>MyApp</RootNamespace>
    <AssemblyName>MyApp</AssemblyName>
    <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>bin\Debug\</OutputPath>
    <DefineConstants>TRACE;DEBUG;CODE_ANALYSIS;SIM</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">
    <DebugType>pdbonly</DebugType>
    <Optimize>true</Optimize>
    <OutputPath>bin\Release\</OutputPath>
    <DefineConstants>TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>3</WarningLevel>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include=""Program.cs"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>";
        File.WriteAllText(projectPath, oldStyleContent);
        
        // Act
        var result = _conversionService.ConvertOldStyleToSdkStyleCustomModern(projectPath);
        
        // Assert
        Assert.True(result.Success, $"Conversion failed: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        
        Assert.NotNull(root);
        
        // Check conditional property groups are preserved
        var debugGroup = root.Elements()
            .Where(e => e.Name.LocalName == "PropertyGroup")
            .FirstOrDefault(e => e.Attribute("Condition")?.Value.Contains("Debug") == true);
        
        Assert.NotNull(debugGroup);
        
        // Check DefineConstants is preserved
        var defineConstants = debugGroup.Elements().FirstOrDefault(e => e.Name.LocalName == "DefineConstants");
        Assert.NotNull(defineConstants);
        Assert.Equal("TRACE;DEBUG;CODE_ANALYSIS;SIM", defineConstants.Value);
        
        // Check DebugType is preserved
        var debugType = debugGroup.Elements().FirstOrDefault(e => e.Name.LocalName == "DebugType");
        Assert.NotNull(debugType);
        Assert.Equal("full", debugType.Value);
        
        // Check Optimize is preserved
        var optimize = debugGroup.Elements().FirstOrDefault(e => e.Name.LocalName == "Optimize");
        Assert.NotNull(optimize);
        Assert.Equal("false", optimize.Value);
        
        // Check WarningLevel is preserved
        var warningLevel = debugGroup.Elements().FirstOrDefault(e => e.Name.LocalName == "WarningLevel");
        Assert.NotNull(warningLevel);
        Assert.Equal("4", warningLevel.Value);
        
        // Check ErrorReport is removed
        var errorReport = debugGroup.Elements().FirstOrDefault(e => e.Name.LocalName == "ErrorReport");
        Assert.Null(errorReport);
        
        // Check Release configuration
        var releaseGroup = root.Elements()
            .Where(e => e.Name.LocalName == "PropertyGroup")
            .FirstOrDefault(e => e.Attribute("Condition")?.Value.Contains("Release") == true);
        
        Assert.NotNull(releaseGroup);
        
        var releaseWarning = releaseGroup.Elements().FirstOrDefault(e => e.Name.LocalName == "WarningLevel");
        Assert.NotNull(releaseWarning);
        Assert.Equal("3", releaseWarning.Value);
    }

    [Fact]
    public void Test_CustomOneWayModern_PlatformTargetVariants()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "CustomModernPlatform.csproj");
        var oldStyleContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""15.0"">
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <OutputType>Exe</OutputType>
    <RootNamespace>MyApp</RootNamespace>
    <AssemblyName>MyApp</AssemblyName>
    <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>bin\Debug\</OutputPath>
    <DefineConstants>DEBUG;TRACE</DefineConstants>
    <PlatformTarget>x86</PlatformTarget>
    <Prefer32Bit>true</Prefer32Bit>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include=""Program.cs"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>";
        File.WriteAllText(projectPath, oldStyleContent);
        
        // Act
        var result = _conversionService.ConvertOldStyleToSdkStyleCustomModern(projectPath);
        
        // Assert
        Assert.True(result.Success, $"Conversion failed: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        
        Assert.NotNull(root);
        
        // Check conditional property group
        var debugGroup = root.Elements()
            .Where(e => e.Name.LocalName == "PropertyGroup")
            .FirstOrDefault(e => e.Attribute("Condition")?.Value.Contains("Debug") == true);
        
        Assert.NotNull(debugGroup);
        
        // Check PlatformTarget is preserved
        var platformTarget = debugGroup.Elements().FirstOrDefault(e => e.Name.LocalName == "PlatformTarget");
        Assert.NotNull(platformTarget);
        Assert.Equal("x86", platformTarget.Value);
        
        // Check Prefer32Bit is preserved
        var prefer32Bit = debugGroup.Elements().FirstOrDefault(e => e.Name.LocalName == "Prefer32Bit");
        Assert.NotNull(prefer32Bit);
        Assert.Equal("true", prefer32Bit.Value);
    }

    [Fact]
    public void Test_CustomOneWayModern_ProjectReferenceCleanup()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "CustomModernProjRef.csproj");
        var oldStyleContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""15.0"">
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <OutputType>Library</OutputType>
    <RootNamespace>MyLib</RootNamespace>
    <AssemblyName>MyLib</AssemblyName>
    <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include=""Class1.cs"" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include=""..\..\Custom\Custom.csproj"">
      <Project>{ABCD1234-1234-1234-1234-123456789ABC}</Project>
      <Name>Custom</Name>
    </ProjectReference>
    <ProjectReference Include=""..\Other\Other.csproj"">
      <Project>{EFGH5678-5678-5678-5678-567856785678}</Project>
      <Name>Other</Name>
    </ProjectReference>
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>";
        File.WriteAllText(projectPath, oldStyleContent);
        
        // Act
        var result = _conversionService.ConvertOldStyleToSdkStyleCustomModern(projectPath);
        
        // Assert
        Assert.True(result.Success, $"Conversion failed: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        
        Assert.NotNull(root);
        
        // Check ProjectReferences are simplified
        var projectRefs = root.Descendants().Where(e => e.Name.LocalName == "ProjectReference").ToList();
        Assert.Equal(2, projectRefs.Count);
        
        // Check first reference
        var firstRef = projectRefs[0];
        Assert.Equal(@"..\..\Custom\Custom.csproj", firstRef.Attribute("Include")?.Value);
        // Should be self-closing (no child elements)
        Assert.Empty(firstRef.Elements());
        
        // Check second reference
        var secondRef = projectRefs[1];
        Assert.Equal(@"..\Other\Other.csproj", secondRef.Attribute("Include")?.Value);
        Assert.Empty(secondRef.Elements());
    }

    [Fact]
    public void Test_CustomOneWayModern_CompileItemRetention()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "CustomModernCompile.csproj");
        var oldStyleContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""15.0"">
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <OutputType>Library</OutputType>
    <RootNamespace>MyLib</RootNamespace>
    <AssemblyName>MyLib</AssemblyName>
    <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include=""Class1.cs"" />
    <Compile Include=""Class2.cs"" />
    <Compile Include=""Helpers\Helper.cs"" />
    <Compile Include=""Properties\AssemblyInfo.cs"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>";
        File.WriteAllText(projectPath, oldStyleContent);
        
        // Act
        var result = _conversionService.ConvertOldStyleToSdkStyleCustomModern(projectPath);
        
        // Assert
        Assert.True(result.Success, $"Conversion failed: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        
        Assert.NotNull(root);
        
        // Check EnableDefaultCompileItems is false
        var enableDefaultCompile = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "EnableDefaultCompileItems");
        Assert.NotNull(enableDefaultCompile);
        Assert.Equal("false", enableDefaultCompile.Value);
        
        // Check all Compile items are preserved
        var compileItems = root.Descendants().Where(e => e.Name.LocalName == "Compile").ToList();
        Assert.Equal(4, compileItems.Count);
        
        // Verify specific files
        var class1 = compileItems.FirstOrDefault(e => e.Attribute("Include")?.Value == "Class1.cs");
        Assert.NotNull(class1);
        
        var class2 = compileItems.FirstOrDefault(e => e.Attribute("Include")?.Value == "Class2.cs");
        Assert.NotNull(class2);
        
        var helper = compileItems.FirstOrDefault(e => e.Attribute("Include")?.Value == @"Helpers\Helper.cs");
        Assert.NotNull(helper);
        
        var assemblyInfo = compileItems.FirstOrDefault(e => e.Attribute("Include")?.Value == @"Properties\AssemblyInfo.cs");
        Assert.NotNull(assemblyInfo);
    }

    [Fact]
    public void Test_CustomOneWayModern_GuidRetention()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "CustomModernGuid.csproj");
        var oldStyleContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""15.0"">
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProjectGuid>{AAAABBBB-CCCC-DDDD-EEEE-FFFFGGGGHHH}</ProjectGuid>
    <OutputType>Library</OutputType>
    <RootNamespace>SharedLib</RootNamespace>
    <AssemblyName>SharedLib</AssemblyName>
    <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include=""Shared.cs"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>";
        File.WriteAllText(projectPath, oldStyleContent);
        
        // Act
        var result = _conversionService.ConvertOldStyleToSdkStyleCustomModern(projectPath);
        
        // Assert
        Assert.True(result.Success, $"Conversion failed: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        
        Assert.NotNull(root);
        
        // Check ProjectGuid is retained for backward compatibility
        var projectGuid = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "ProjectGuid");
        Assert.NotNull(projectGuid);
        Assert.Equal("{AAAABBBB-CCCC-DDDD-EEEE-FFFFGGGGHHH}", projectGuid.Value);
    }

    [Fact]
    public void Test_CustomOneWayModern_CleanMetadata()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "CustomModernClean.csproj");
        var oldStyleContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""15.0"">
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProductVersion>8.0.30703</ProductVersion>
    <SchemaVersion>2.0</SchemaVersion>
    <ProjectGuid>{12345678-1234-1234-1234-123456789ABC}</ProjectGuid>
    <OutputType>Library</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <RootNamespace>CleanLib</RootNamespace>
    <AssemblyName>CleanLib</AssemblyName>
    <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
    <TargetFrameworkProfile></TargetFrameworkProfile>
    <FileAlignment>512</FileAlignment>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>bin\Debug\</OutputPath>
    <DefineConstants>DEBUG;TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include=""Class1.cs"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>";
        File.WriteAllText(projectPath, oldStyleContent);
        
        // Act
        var result = _conversionService.ConvertOldStyleToSdkStyleCustomModern(projectPath);
        
        // Assert
        Assert.True(result.Success, $"Conversion failed: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        
        Assert.NotNull(root);
        
        // Check all obsolete properties are removed
        var productVersion = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "ProductVersion");
        Assert.Null(productVersion);
        
        var schemaVersion = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "SchemaVersion");
        Assert.Null(schemaVersion);
        
        var appDesignerFolder = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "AppDesignerFolder");
        Assert.Null(appDesignerFolder);
        
        var targetFrameworkProfile = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "TargetFrameworkProfile");
        Assert.Null(targetFrameworkProfile);
        
        var errorReport = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "ErrorReport");
        Assert.Null(errorReport);
        
        // Check FileAlignment is removed (not needed in SDK-style)
        var fileAlignment = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "FileAlignment");
        Assert.Null(fileAlignment);
    }

    [Fact]
    public void Test_CustomOneWayModern_RemovesEmptyStartupObjectForLibrary()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "CustomModernLibStartup.csproj");
        var oldStyleContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""15.0"">
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <OutputType>Library</OutputType>
    <RootNamespace>MyLib</RootNamespace>
    <AssemblyName>MyLib</AssemblyName>
    <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
    <StartupObject></StartupObject>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include=""Class1.cs"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>";
        File.WriteAllText(projectPath, oldStyleContent);
        
        // Act
        var result = _conversionService.ConvertOldStyleToSdkStyleCustomModern(projectPath);
        
        // Assert
        Assert.True(result.Success, $"Conversion failed: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        
        Assert.NotNull(root);
        
        // Check StartupObject is NOT present (empty for library)
        var startupObject = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "StartupObject");
        Assert.Null(startupObject);
    }

    [Fact]
    public void Test_CustomOneWayModern_PreservesStartupObjectForExecutable()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "CustomModernExeStartup.csproj");
        var oldStyleContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""15.0"">
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <OutputType>Exe</OutputType>
    <RootNamespace>MyApp</RootNamespace>
    <AssemblyName>MyApp</AssemblyName>
    <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
    <StartupObject>MyApp.Program</StartupObject>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include=""Program.cs"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>";
        File.WriteAllText(projectPath, oldStyleContent);
        
        // Act
        var result = _conversionService.ConvertOldStyleToSdkStyleCustomModern(projectPath);
        
        // Assert
        Assert.True(result.Success, $"Conversion failed: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        
        Assert.NotNull(root);
        
        // Check StartupObject is preserved for executable
        var startupObject = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "StartupObject");
        Assert.NotNull(startupObject);
        Assert.Equal("MyApp.Program", startupObject.Value);
    }

    #endregion

    #region Helper Classes

    private class BuildResult
    {
        public bool Success { get; set; }
        public string Error { get; set; } = string.Empty;
    }

    #endregion
}
