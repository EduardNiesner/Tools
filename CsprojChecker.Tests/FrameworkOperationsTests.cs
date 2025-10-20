using System.Xml.Linq;
using Xunit;
using CsprojChecker.Core;
using CsprojChecker.Core.Models;

namespace CsprojChecker.Tests;

/// <summary>
/// Tests for Change Target Framework and Append Target Framework operations
/// </summary>
public class FrameworkOperationsTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly ProjectConversionService _conversionService;
    
    public FrameworkOperationsTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "CsprojCheckerFrameworkTests_" + Guid.NewGuid().ToString("N"));
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

    #region Change Target Framework Tests

    [Fact]
    public void ChangeTargetFramework_SingleTarget_UpdatesSuccessfully()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "SingleTarget.csproj");
        var content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
</Project>";
        File.WriteAllText(projectPath, content);
        
        // Act
        var result = ChangeTargetFramework(projectPath, "net8.0");
        
        // Assert
        Assert.True(result.Success, $"Change failed: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        Assert.NotNull(root);
        
        var tfm = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "TargetFramework");
        Assert.NotNull(tfm);
        Assert.Equal("net8.0", tfm.Value);
    }

    [Fact]
    public void ChangeTargetFramework_MultipleTargets_UpdatesSuccessfully()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "MultiTarget.csproj");
        var content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFrameworks>net6.0;net7.0</TargetFrameworks>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
</Project>";
        File.WriteAllText(projectPath, content);
        
        // Act
        var result = ChangeTargetFramework(projectPath, "net8.0;net9.0");
        
        // Assert
        Assert.True(result.Success, $"Change failed: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        Assert.NotNull(root);
        
        var tfms = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "TargetFrameworks");
        Assert.NotNull(tfms);
        Assert.Equal("net8.0;net9.0", tfms.Value);
    }

    [Fact]
    public void ChangeTargetFramework_SingleToMultiple_ConvertsProperty()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "SingleToMulti.csproj");
        var content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
</Project>";
        File.WriteAllText(projectPath, content);
        
        // Act
        var result = ChangeTargetFramework(projectPath, "net6.0;net8.0");
        
        // Assert
        Assert.True(result.Success, $"Change failed: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        Assert.NotNull(root);
        
        // Should now have TargetFrameworks (plural)
        var tfms = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "TargetFrameworks");
        Assert.NotNull(tfms);
        Assert.Equal("net6.0;net8.0", tfms.Value);
        
        // Should not have TargetFramework (singular)
        var tfm = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "TargetFramework");
        Assert.Null(tfm);
    }

    [Fact]
    public void ChangeTargetFramework_MultipleToSingle_ConvertsProperty()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "MultiToSingle.csproj");
        var content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFrameworks>net6.0;net7.0;net8.0</TargetFrameworks>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
</Project>";
        File.WriteAllText(projectPath, content);
        
        // Act
        var result = ChangeTargetFramework(projectPath, "net9.0");
        
        // Assert
        Assert.True(result.Success, $"Change failed: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        Assert.NotNull(root);
        
        // Should now have TargetFramework (singular)
        var tfm = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "TargetFramework");
        Assert.NotNull(tfm);
        Assert.Equal("net9.0", tfm.Value);
        
        // Should not have TargetFrameworks (plural)
        var tfms = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "TargetFrameworks");
        Assert.Null(tfms);
    }

    [Fact]
    public void ChangeTargetFramework_VariableToken_PreservesVariable()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "VariableToken.csproj");
        var content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
</Project>";
        File.WriteAllText(projectPath, content);
        
        // Act
        var result = ChangeTargetFramework(projectPath, "$(CustomFramework)");
        
        // Assert
        Assert.True(result.Success, $"Change failed: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        Assert.NotNull(root);
        
        var tfm = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "TargetFramework");
        Assert.NotNull(tfm);
        Assert.Equal("$(CustomFramework)", tfm.Value);
    }

    [Fact]
    public void ChangeTargetFramework_MultiplePropertyGroups_UpdatesAll()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "MultiPropertyGroup.csproj");
        var content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
  <PropertyGroup Condition=""'$(Configuration)'=='Debug'"">
    <TargetFramework>net6.0</TargetFramework>
  </PropertyGroup>
  <PropertyGroup Condition=""'$(Configuration)'=='Release'"">
    <TargetFramework>net6.0</TargetFramework>
  </PropertyGroup>
</Project>";
        File.WriteAllText(projectPath, content);
        
        // Act
        var result = ChangeTargetFramework(projectPath, "net8.0");
        
        // Assert
        Assert.True(result.Success, $"Change failed: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        Assert.NotNull(root);
        
        // Check that ALL PropertyGroups with TargetFramework are updated
        var allTfms = root.Descendants().Where(e => e.Name.LocalName == "TargetFramework").ToList();
        Assert.NotEmpty(allTfms);
        
        foreach (var tfm in allTfms)
        {
            Assert.Equal("net8.0", tfm.Value);
        }
    }

    [Fact]
    public void ChangeTargetFramework_PreservesUnrelatedProperties()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "UnrelatedProps.csproj");
        var content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
    <CustomProperty>CustomValue</CustomProperty>
  </PropertyGroup>
</Project>";
        File.WriteAllText(projectPath, content);
        
        // Act
        var result = ChangeTargetFramework(projectPath, "net8.0");
        
        // Assert
        Assert.True(result.Success, $"Change failed: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        Assert.NotNull(root);
        
        // Verify TFM was changed
        var tfm = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "TargetFramework");
        Assert.NotNull(tfm);
        Assert.Equal("net8.0", tfm.Value);
        
        // Verify unrelated properties are preserved
        var nullable = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "Nullable");
        Assert.NotNull(nullable);
        Assert.Equal("enable", nullable.Value);
        
        var langVersion = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "LangVersion");
        Assert.NotNull(langVersion);
        Assert.Equal("latest", langVersion.Value);
        
        var customProp = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "CustomProperty");
        Assert.NotNull(customProp);
        Assert.Equal("CustomValue", customProp.Value);
    }

    #endregion

    #region Append Target Framework Tests

    [Fact]
    public void AppendTargetFramework_SingleTarget_AddsNewFramework()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "AppendSingle.csproj");
        var content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
</Project>";
        File.WriteAllText(projectPath, content);
        
        // Act
        var result = AppendTargetFramework(projectPath, "net8.0");
        
        // Assert
        Assert.True(result.Success, $"Append failed: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        Assert.NotNull(root);
        
        // Should now have TargetFrameworks (plural)
        var tfms = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "TargetFrameworks");
        Assert.NotNull(tfms);
        
        // Both frameworks should be present
        var frameworks = tfms.Value.Split(';');
        Assert.Contains("net6.0", frameworks);
        Assert.Contains("net8.0", frameworks);
    }

    [Fact]
    public void AppendTargetFramework_MultipleTargets_AddsNewFramework()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "AppendMulti.csproj");
        var content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFrameworks>net6.0;net7.0</TargetFrameworks>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
</Project>";
        File.WriteAllText(projectPath, content);
        
        // Act
        var result = AppendTargetFramework(projectPath, "net9.0");
        
        // Assert
        Assert.True(result.Success, $"Append failed: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        Assert.NotNull(root);
        
        var tfms = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "TargetFrameworks");
        Assert.NotNull(tfms);
        
        // All frameworks should be present
        var frameworks = tfms.Value.Split(';');
        Assert.Contains("net6.0", frameworks);
        Assert.Contains("net7.0", frameworks);
        Assert.Contains("net9.0", frameworks);
    }

    [Fact]
    public void AppendTargetFramework_DuplicateFramework_DoesNotDuplicate()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "AppendDuplicate.csproj");
        var content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFrameworks>net6.0;net7.0</TargetFrameworks>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
</Project>";
        File.WriteAllText(projectPath, content);
        
        // Act - Try to append net6.0 (case-insensitive match)
        var result = AppendTargetFramework(projectPath, "NET6.0");
        
        // Assert
        Assert.True(result.Success, $"Append failed: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        Assert.NotNull(root);
        
        var tfms = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "TargetFrameworks");
        Assert.NotNull(tfms);
        
        // Should deduplicate case-insensitively for literals
        var frameworks = tfms.Value.Split(';');
        Assert.Equal(2, frameworks.Length); // Still only 2 frameworks
        Assert.Contains("net6.0", frameworks, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("net7.0", frameworks, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void AppendTargetFramework_VariableDuplication_ExactMatchOnly()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "AppendVariableDup.csproj");
        var content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFrameworks>$(MyFramework);net6.0</TargetFrameworks>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
</Project>";
        File.WriteAllText(projectPath, content);
        
        // Act - Variables require exact match
        var result = AppendTargetFramework(projectPath, "$(MyFramework)");
        
        // Assert
        Assert.True(result.Success, $"Append failed: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        Assert.NotNull(root);
        
        var tfms = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "TargetFrameworks");
        Assert.NotNull(tfms);
        
        // Should deduplicate with exact match for variables
        var frameworks = tfms.Value.Split(';');
        Assert.Equal(2, frameworks.Length); // Still only 2 frameworks
        Assert.Contains("$(MyFramework)", frameworks);
        Assert.Contains("net6.0", frameworks, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void AppendTargetFramework_Net5PlusWinForms_AddsWindowsSuffix()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "AppendWinForms.csproj");
        var content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net48-windows</TargetFramework>
    <OutputType>WinExe</OutputType>
    <UseWindowsForms>true</UseWindowsForms>
  </PropertyGroup>
</Project>";
        File.WriteAllText(projectPath, content);
        
        // Act
        var result = AppendTargetFramework(projectPath, "net8.0");
        
        // Assert
        Assert.True(result.Success, $"Append failed: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        Assert.NotNull(root);
        
        var tfms = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "TargetFrameworks");
        Assert.NotNull(tfms);
        
        // net8.0 should become net8.0-windows for WinForms
        var frameworks = tfms.Value.Split(';');
        Assert.Contains("net48-windows", frameworks);
        Assert.Contains("net8.0-windows", frameworks);
    }

    [Fact]
    public void AppendTargetFramework_MixedVariablesAndLiterals_SortsCorrectly()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "AppendMixed.csproj");
        var content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
</Project>";
        File.WriteAllText(projectPath, content);
        
        // Act - append a variable
        var result = AppendTargetFramework(projectPath, "$(CustomFramework)");
        
        // Assert
        Assert.True(result.Success, $"Append failed: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        Assert.NotNull(root);
        
        var tfms = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "TargetFrameworks");
        Assert.NotNull(tfms);
        
        // Variables should come first
        var frameworks = tfms.Value.Split(';');
        Assert.Equal(2, frameworks.Length);
        Assert.Equal("$(CustomFramework)", frameworks[0]); // Variable first
        Assert.Equal("net6.0", frameworks[1]); // Then literal
        
        // Note: We don't require total ordering for literals, just variables-first
    }

    [Fact]
    public void AppendTargetFramework_OldStyleProject_Fails()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "AppendOldStyle.csproj");
        var content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""15.0"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
    <RootNamespace>ConsoleApp1</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""System"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>";
        File.WriteAllText(projectPath, content);
        
        // Act
        var result = AppendTargetFramework(projectPath, "net9.0");
        
        // Assert - should fail for Old-style projects
        Assert.False(result.Success);
        Assert.Contains("SDK-style", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Helper Methods

    private OperationResult ChangeTargetFramework(string projectPath, string newFramework)
    {
        return _conversionService.ChangeTargetFramework(projectPath, newFramework);
    }

    private OperationResult AppendTargetFramework(string projectPath, string frameworkToAppend)
    {
        return _conversionService.AppendTargetFramework(projectPath, frameworkToAppend);
    }

    #endregion
}
