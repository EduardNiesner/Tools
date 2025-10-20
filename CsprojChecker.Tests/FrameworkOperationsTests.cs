using System.Xml.Linq;
using Xunit;

namespace CsprojChecker.Tests;

/// <summary>
/// Tests for Change Target Framework and Append Target Framework operations
/// </summary>
public class FrameworkOperationsTests : IDisposable
{
    private readonly string _testDirectory;
    
    public FrameworkOperationsTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "CsprojCheckerFrameworkTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);
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
        
        // Act
        var result = AppendTargetFramework(projectPath, "net6.0");
        
        // Assert
        Assert.True(result.Success, $"Append failed: {result.Error}");
        
        var doc = XDocument.Load(projectPath);
        var root = doc.Root;
        Assert.NotNull(root);
        
        var tfms = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "TargetFrameworks");
        Assert.NotNull(tfms);
        
        // Should only have 2 frameworks, not 3
        var frameworks = tfms.Value.Split(';');
        Assert.Equal(2, frameworks.Length);
        Assert.Contains("net6.0", frameworks);
        Assert.Contains("net7.0", frameworks);
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
        
        // Variables should come first, then literals sorted by version descending
        var frameworks = tfms.Value.Split(';');
        Assert.Equal("$(CustomFramework)", frameworks[0]); // Variable first
        Assert.Equal("net6.0", frameworks[1]); // Then literal
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
        try
        {
            var doc = XDocument.Load(projectPath);
            var root = doc.Root;
            
            if (root == null)
                return new OperationResult { Success = false, Error = "Invalid project file" };
            
            XNamespace ns = root.GetDefaultNamespace();
            
            // Find existing TFM property
            var tfmElement = root.Descendants(ns + "TargetFramework").FirstOrDefault();
            var tfmsElement = root.Descendants(ns + "TargetFrameworks").FirstOrDefault();
            
            // Determine if new framework is single or multiple
            bool isMultiple = newFramework.Contains(';');
            
            if (isMultiple)
            {
                // Remove TargetFramework (singular) if exists
                tfmElement?.Remove();
                
                // Update or add TargetFrameworks (plural)
                if (tfmsElement != null)
                {
                    tfmsElement.Value = newFramework;
                }
                else
                {
                    var propertyGroup = root.Descendants(ns + "PropertyGroup").FirstOrDefault();
                    if (propertyGroup != null)
                    {
                        propertyGroup.Add(new XElement(ns + "TargetFrameworks", newFramework));
                    }
                }
            }
            else
            {
                // Remove TargetFrameworks (plural) if exists
                tfmsElement?.Remove();
                
                // Update or add TargetFramework (singular)
                if (tfmElement != null)
                {
                    tfmElement.Value = newFramework;
                }
                else
                {
                    var propertyGroup = root.Descendants(ns + "PropertyGroup").FirstOrDefault();
                    if (propertyGroup != null)
                    {
                        propertyGroup.Add(new XElement(ns + "TargetFramework", newFramework));
                    }
                }
            }
            
            doc.Save(projectPath);
            return new OperationResult { Success = true };
        }
        catch (Exception ex)
        {
            return new OperationResult { Success = false, Error = ex.Message };
        }
    }

    private OperationResult AppendTargetFramework(string projectPath, string frameworkToAppend)
    {
        try
        {
            var doc = XDocument.Load(projectPath);
            var root = doc.Root;
            
            if (root == null)
                return new OperationResult { Success = false, Error = "Invalid project file" };
            
            // Check if SDK-style
            if (root.Attribute("Sdk") == null)
                return new OperationResult { Success = false, Error = "Append only works with SDK-style projects" };
            
            XNamespace ns = root.GetDefaultNamespace();
            
            // Check if WinForms
            var useWinForms = root.Descendants(ns + "UseWindowsForms").FirstOrDefault();
            bool isWinForms = useWinForms != null && useWinForms.Value.Equals("true", StringComparison.OrdinalIgnoreCase);
            
            // Find existing TFM property
            var tfmElement = root.Descendants(ns + "TargetFramework").FirstOrDefault();
            var tfmsElement = root.Descendants(ns + "TargetFrameworks").FirstOrDefault();
            
            // Get current frameworks
            List<string> currentFrameworks = new List<string>();
            if (tfmsElement != null)
            {
                currentFrameworks = tfmsElement.Value.Split(';').ToList();
            }
            else if (tfmElement != null)
            {
                currentFrameworks.Add(tfmElement.Value);
            }
            
            // Process framework to append
            string processedFramework = frameworkToAppend;
            
            // Add -windows suffix for WinForms projects with net5.0+ literal frameworks
            if (isWinForms && !processedFramework.StartsWith("$") && 
                (processedFramework.StartsWith("net5") || processedFramework.StartsWith("net6") || 
                 processedFramework.StartsWith("net7") || processedFramework.StartsWith("net8") || 
                 processedFramework.StartsWith("net9")))
            {
                if (!processedFramework.EndsWith("-windows"))
                {
                    processedFramework += "-windows";
                }
            }
            
            // Add to list if not already present (case-insensitive for literals, exact for variables)
            bool isDuplicate = false;
            if (processedFramework.StartsWith("$"))
            {
                // Exact match for variables
                isDuplicate = currentFrameworks.Contains(processedFramework);
            }
            else
            {
                // Case-insensitive match for literals
                isDuplicate = currentFrameworks.Any(f => f.Equals(processedFramework, StringComparison.OrdinalIgnoreCase));
            }
            
            if (!isDuplicate)
            {
                currentFrameworks.Add(processedFramework);
            }
            
            // Sort: variables first, then literals by version descending
            var sortedFrameworks = currentFrameworks
                .OrderByDescending(f => f.StartsWith("$")) // Variables first
                .ThenByDescending(f => f) // Then by name (which roughly sorts by version)
                .ToList();
            
            // Update project
            string newTfms = string.Join(";", sortedFrameworks);
            
            if (sortedFrameworks.Count > 1)
            {
                // Use TargetFrameworks (plural)
                tfmElement?.Remove();
                
                if (tfmsElement != null)
                {
                    tfmsElement.Value = newTfms;
                }
                else
                {
                    var propertyGroup = root.Descendants(ns + "PropertyGroup").FirstOrDefault();
                    if (propertyGroup != null)
                    {
                        propertyGroup.Add(new XElement(ns + "TargetFrameworks", newTfms));
                    }
                }
            }
            else
            {
                // Use TargetFramework (singular)
                tfmsElement?.Remove();
                
                if (tfmElement != null)
                {
                    tfmElement.Value = newTfms;
                }
                else
                {
                    var propertyGroup = root.Descendants(ns + "PropertyGroup").FirstOrDefault();
                    if (propertyGroup != null)
                    {
                        propertyGroup.Add(new XElement(ns + "TargetFramework", newTfms));
                    }
                }
            }
            
            doc.Save(projectPath);
            return new OperationResult { Success = true };
        }
        catch (Exception ex)
        {
            return new OperationResult { Success = false, Error = ex.Message };
        }
    }

    #endregion

    #region Helper Classes

    private class OperationResult
    {
        public bool Success { get; set; }
        public string Error { get; set; } = string.Empty;
    }

    #endregion
}
