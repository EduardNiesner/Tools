using System.Xml.Linq;
using Xunit;
using CsProjConverter.Core;

namespace CsProjConverter.Tests;

/// <summary>
/// Tests to verify XML formatting and indentation in converted project files
/// </summary>
public class XmlFormattingTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly ProjectConversionService _conversionService;
    
    public XmlFormattingTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "XmlFormattingTests_" + Guid.NewGuid().ToString("N"));
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

    [Fact]
    public void ConvertedXml_HasProperIndentation()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "TestProject.csproj");
        var oldStyleContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""15.0"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
    <RootNamespace>TestApp</RootNamespace>
    <AssemblyName>TestApp</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""System"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>";
        File.WriteAllText(projectPath, oldStyleContent);
        
        // Act
        var result = _conversionService.ConvertOldStyleToSdkStyle(projectPath);
        
        // Assert
        Assert.True(result.Success, $"Conversion failed: {result.Error}");
        
        // Read the converted file
        var convertedContent = File.ReadAllText(projectPath);
        var lines = convertedContent.Split('\n');
        
        // Verify that closing tags are on their own lines with proper indentation
        bool foundPropertyGroup = false;
        bool foundTargetFramework = false;
        
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r', '\n');
            
            // Check that PropertyGroup opening tag exists
            if (line.Contains("<PropertyGroup>"))
            {
                foundPropertyGroup = true;
                Assert.Equal("  <PropertyGroup>", line);
            }
            
            // Check that TargetFramework element is properly indented
            if (line.Contains("<TargetFramework>"))
            {
                foundTargetFramework = true;
                Assert.StartsWith("    <TargetFramework>", line);
                Assert.EndsWith("</TargetFramework>", line);
            }
            
            // Check that closing PropertyGroup tag is on its own line with proper indentation
            if (line.Contains("</PropertyGroup>"))
            {
                Assert.Equal("  </PropertyGroup>", line);
                // Verify it's not on the same line as another closing tag
                Assert.DoesNotContain("</TargetFramework>", line);
                Assert.DoesNotContain("</OutputType>", line);
                Assert.DoesNotContain("</RootNamespace>", line);
            }
            
            // Check that closing Project tag is on its own line
            if (line == "</Project>")
            {
                // Should not have any other content on the same line
                Assert.Equal("</Project>", line);
            }
        }
        
        Assert.True(foundPropertyGroup, "PropertyGroup element not found");
        Assert.True(foundTargetFramework, "TargetFramework element not found");
    }

    [Fact]
    public void ConvertedXml_UsesConsistentLineEndings()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "TestProject2.csproj");
        var oldStyleContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""15.0"">
  <PropertyGroup>
    <TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
  </PropertyGroup>
</Project>";
        File.WriteAllText(projectPath, oldStyleContent);
        
        // Act
        var result = _conversionService.ConvertOldStyleToSdkStyle(projectPath);
        
        // Assert
        Assert.True(result.Success, $"Conversion failed: {result.Error}");
        
        // Read the file as bytes to check line endings
        var bytes = File.ReadAllBytes(projectPath);
        
        // Count CR and LF characters
        int crCount = 0;
        int lfCount = 0;
        for (int i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] == 0x0D) crCount++; // CR (\r)
            if (bytes[i] == 0x0A) lfCount++; // LF (\n)
        }
        
        // With NewLineChars = "\n" and NewLineHandling = Replace,
        // we should have LF-only line endings (no CR)
        Assert.Equal(0, crCount);
        Assert.True(lfCount > 0, "File should contain LF characters");
    }

    [Fact]
    public void ChangeTargetFramework_MaintainsFormatting()
    {
        // Arrange
        var projectPath = Path.Combine(_testDirectory, "TestProject3.csproj");
        var sdkStyleContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
</Project>";
        File.WriteAllText(projectPath, sdkStyleContent);
        
        // Act
        var result = _conversionService.ChangeTargetFramework(projectPath, "net9.0");
        
        // Assert
        Assert.True(result.Success, $"Operation failed: {result.Error}");
        
        // Read the converted file
        var convertedContent = File.ReadAllText(projectPath);
        var lines = convertedContent.Split('\n');
        
        // Verify proper indentation is maintained
        bool foundTargetFramework = false;
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r', '\n');
            
            if (line.Contains("<TargetFramework>"))
            {
                foundTargetFramework = true;
                Assert.StartsWith("    <TargetFramework>", line);
                Assert.Contains("net9.0", line);
            }
            
            if (line.Contains("</PropertyGroup>"))
            {
                Assert.Equal("  </PropertyGroup>", line);
            }
        }
        
        Assert.True(foundTargetFramework, "TargetFramework element not found");
    }
}
