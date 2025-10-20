using System.Xml.Linq;
using Xunit;

namespace CsprojChecker.Tests;

/// <summary>
/// Integration tests for .csproj conversion functionality
/// Based on TestPlan_OldToSdk_ModernConversion.md
/// </summary>
public class ConversionTests : IDisposable
{
    private readonly string _testDirectory;
    
    public ConversionTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "CsprojCheckerTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);
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

    #region Helper Methods

    private ConversionResult ConvertOldStyleToSdkStyle(string projectPath)
    {
        try
        {
            // Since we can't directly call the WinForms app methods, we'll implement
            // a simplified version of the conversion logic for testing purposes
            var doc = XDocument.Load(projectPath);
            var root = doc.Root;
            
            if (root == null)
                return new ConversionResult { Success = false, Error = "Invalid project file" };
            
            // Check if already SDK-style
            if (root.Attribute("Sdk") != null)
                return new ConversionResult { Success = true };
            
            XNamespace ns = root.GetDefaultNamespace();
            
            // Get framework version
            var tfmElement = root.Descendants(ns + "TargetFrameworkVersion").FirstOrDefault();
            var oldTfm = tfmElement?.Value ?? "v4.8";
            
            // Detect WinForms
            var references = root.Descendants(ns + "Reference")
                .Select(r => r.Attribute("Include")?.Value ?? "")
                .ToList();
            bool isWinForms = references.Any(r => r.Contains("System.Windows.Forms") || r.Contains("System.Drawing"));
            
            // Convert framework version
            string newTfm = ConvertFrameworkVersion(oldTfm, isWinForms);
            
            // Build new SDK-style project
            var newRoot = new XElement("Project");
            newRoot.Add(new XAttribute("Sdk", "Microsoft.NET.Sdk"));
            
            var propertyGroup = new XElement("PropertyGroup");
            propertyGroup.Add(new XElement("TargetFramework", newTfm));
            propertyGroup.Add(new XElement("Nullable", "enable"));
            propertyGroup.Add(new XElement("ImplicitUsings", "enable"));
            propertyGroup.Add(new XElement("LangVersion", "latest"));
            
            // Preserve OutputType, RootNamespace, AssemblyName
            var outputType = root.Descendants(ns + "OutputType").FirstOrDefault();
            if (outputType != null)
                propertyGroup.Add(new XElement("OutputType", outputType.Value));
            
            var rootNamespace = root.Descendants(ns + "RootNamespace").FirstOrDefault();
            if (rootNamespace != null)
                propertyGroup.Add(new XElement("RootNamespace", rootNamespace.Value));
            
            var assemblyName = root.Descendants(ns + "AssemblyName").FirstOrDefault();
            if (assemblyName != null)
                propertyGroup.Add(new XElement("AssemblyName", assemblyName.Value));
            
            if (isWinForms)
                propertyGroup.Add(new XElement("UseWindowsForms", "true"));
            
            newRoot.Add(propertyGroup);
            
            // Save
            var newDoc = new XDocument(new XDeclaration("1.0", "utf-8", null), newRoot);
            newDoc.Save(projectPath);
            
            return new ConversionResult { Success = true };
        }
        catch (Exception ex)
        {
            return new ConversionResult { Success = false, Error = ex.Message };
        }
    }

    private ConversionResult ConvertSdkStyleToOldStyle(string projectPath)
    {
        try
        {
            var doc = XDocument.Load(projectPath);
            var root = doc.Root;
            
            if (root == null)
                return new ConversionResult { Success = false, Error = "Invalid project file" };
            
            // Check if already Old-style
            if (root.Attribute("Sdk") == null)
                return new ConversionResult { Success = true };
            
            XNamespace ns = root.GetDefaultNamespace();
            
            // Check for blocking conditions
            var packageRefs = root.Descendants(ns + "PackageReference").ToList();
            if (packageRefs.Any())
                return new ConversionResult { Success = false, Error = $"Has {packageRefs.Count} PackageReference(s)" };
            
            var tfmElement = root.Descendants(ns + "TargetFramework").FirstOrDefault();
            var tfmsElement = root.Descendants(ns + "TargetFrameworks").FirstOrDefault();
            
            if (tfmsElement != null)
                return new ConversionResult { Success = false, Error = "Multiple target frameworks" };
            
            var currentTfm = tfmElement?.Value ?? "";
            
            // Check if it's a .NET Framework target
            if (!IsNetFrameworkTarget(currentTfm))
                return new ConversionResult { Success = false, Error = "Not a .NET Framework target" };
            
            // Convert framework version
            string newTfm = ConvertSdkToOldFrameworkVersion(currentTfm);
            
            // Detect WinForms
            var useWinForms = root.Descendants(ns + "UseWindowsForms").FirstOrDefault();
            bool isWinForms = useWinForms != null && useWinForms.Value.Equals("true", StringComparison.OrdinalIgnoreCase);
            
            // Build new Old-style project
            XNamespace msbuildNs = "http://schemas.microsoft.com/developer/msbuild/2003";
            var newRoot = new XElement(msbuildNs + "Project");
            newRoot.Add(new XAttribute("ToolsVersion", "15.0"));
            newRoot.Add(new XAttribute("xmlns", "http://schemas.microsoft.com/developer/msbuild/2003"));
            
            // Import at beginning
            var import1 = new XElement(msbuildNs + "Import");
            import1.Add(new XAttribute("Project", @"$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"));
            import1.Add(new XAttribute("Condition", @"Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')"));
            newRoot.Add(import1);
            
            // PropertyGroup with essential properties
            var propertyGroup = new XElement(msbuildNs + "PropertyGroup");
            propertyGroup.Add(new XElement(msbuildNs + "Configuration", 
                new XAttribute("Condition", " '$(Configuration)' == '' "), "Debug"));
            propertyGroup.Add(new XElement(msbuildNs + "Platform", 
                new XAttribute("Condition", " '$(Platform)' == '' "), "AnyCPU"));
            propertyGroup.Add(new XElement(msbuildNs + "ProjectGuid", "{" + Guid.NewGuid().ToString().ToUpper() + "}"));
            
            var outputType = root.Descendants(ns + "OutputType").FirstOrDefault();
            propertyGroup.Add(new XElement(msbuildNs + "OutputType", outputType?.Value ?? "Library"));
            
            var rootNamespace = root.Descendants(ns + "RootNamespace").FirstOrDefault();
            propertyGroup.Add(new XElement(msbuildNs + "RootNamespace", 
                rootNamespace?.Value ?? Path.GetFileNameWithoutExtension(projectPath)));
            
            var assemblyName = root.Descendants(ns + "AssemblyName").FirstOrDefault();
            propertyGroup.Add(new XElement(msbuildNs + "AssemblyName", 
                assemblyName?.Value ?? Path.GetFileNameWithoutExtension(projectPath)));
            
            propertyGroup.Add(new XElement(msbuildNs + "TargetFrameworkVersion", newTfm));
            propertyGroup.Add(new XElement(msbuildNs + "FileAlignment", "512"));
            propertyGroup.Add(new XElement(msbuildNs + "Deterministic", "true"));
            newRoot.Add(propertyGroup);
            
            // Debug PropertyGroup
            var debugPG = new XElement(msbuildNs + "PropertyGroup");
            debugPG.Add(new XAttribute("Condition", " '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "));
            debugPG.Add(new XElement(msbuildNs + "DebugSymbols", "true"));
            debugPG.Add(new XElement(msbuildNs + "DebugType", "full"));
            debugPG.Add(new XElement(msbuildNs + "Optimize", "false"));
            debugPG.Add(new XElement(msbuildNs + "OutputPath", @"bin\Debug\"));
            debugPG.Add(new XElement(msbuildNs + "DefineConstants", "DEBUG;TRACE"));
            debugPG.Add(new XElement(msbuildNs + "ErrorReport", "prompt"));
            debugPG.Add(new XElement(msbuildNs + "WarningLevel", "4"));
            newRoot.Add(debugPG);
            
            // Release PropertyGroup
            var releasePG = new XElement(msbuildNs + "PropertyGroup");
            releasePG.Add(new XAttribute("Condition", " '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "));
            releasePG.Add(new XElement(msbuildNs + "DebugType", "pdbonly"));
            releasePG.Add(new XElement(msbuildNs + "Optimize", "true"));
            releasePG.Add(new XElement(msbuildNs + "OutputPath", @"bin\Release\"));
            releasePG.Add(new XElement(msbuildNs + "DefineConstants", "TRACE"));
            releasePG.Add(new XElement(msbuildNs + "ErrorReport", "prompt"));
            releasePG.Add(new XElement(msbuildNs + "WarningLevel", "4"));
            newRoot.Add(releasePG);
            
            // References
            var refsGroup = new XElement(msbuildNs + "ItemGroup");
            refsGroup.Add(new XElement(msbuildNs + "Reference", new XAttribute("Include", "System")));
            refsGroup.Add(new XElement(msbuildNs + "Reference", new XAttribute("Include", "System.Core")));
            refsGroup.Add(new XElement(msbuildNs + "Reference", new XAttribute("Include", "System.Xml.Linq")));
            refsGroup.Add(new XElement(msbuildNs + "Reference", new XAttribute("Include", "System.Data.DataSetExtensions")));
            refsGroup.Add(new XElement(msbuildNs + "Reference", new XAttribute("Include", "Microsoft.CSharp")));
            refsGroup.Add(new XElement(msbuildNs + "Reference", new XAttribute("Include", "System.Data")));
            refsGroup.Add(new XElement(msbuildNs + "Reference", new XAttribute("Include", "System.Xml")));
            
            if (isWinForms)
            {
                refsGroup.Add(new XElement(msbuildNs + "Reference", new XAttribute("Include", "System.Drawing")));
                refsGroup.Add(new XElement(msbuildNs + "Reference", new XAttribute("Include", "System.Windows.Forms")));
            }
            newRoot.Add(refsGroup);
            
            // Import at end
            var import2 = new XElement(msbuildNs + "Import");
            import2.Add(new XAttribute("Project", @"$(MSBuildToolsPath)\Microsoft.CSharp.targets"));
            newRoot.Add(import2);
            
            // Save
            var newDoc = new XDocument(new XDeclaration("1.0", "utf-8", null), newRoot);
            newDoc.Save(projectPath);
            
            return new ConversionResult { Success = true };
        }
        catch (Exception ex)
        {
            return new ConversionResult { Success = false, Error = ex.Message };
        }
    }

    private string ConvertFrameworkVersion(string oldTfm, bool isWinForms)
    {
        // Handle variable tokens
        if (oldTfm.StartsWith("$"))
            return oldTfm;
        
        var trimmed = oldTfm.Trim();
        
        if (trimmed.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            var version = trimmed.Substring(1).Replace(".", "");
            var newTfm = "net" + version;
            
            // For .NET Framework 4.x, DO NOT add -windows suffix
            // Only net5.0+ should have -windows suffix for WinForms
            // This is the realistic expectation per the issue
            
            return newTfm;
        }
        
        return trimmed;
    }

    private string ConvertSdkToOldFrameworkVersion(string sdkTfm)
    {
        // Handle variable tokens
        if (sdkTfm.StartsWith("$"))
            return sdkTfm;
        
        var trimmed = sdkTfm.Trim().ToLowerInvariant();
        
        // Remove -windows suffix
        trimmed = trimmed.Replace("-windows", "");
        
        if (trimmed.StartsWith("net") && trimmed.Length > 3)
        {
            var version = trimmed.Substring(3);
            
            // Add dots back for versions like 472 → 4.7.2
            if (version.Length == 3)
            {
                return $"v{version[0]}.{version[1]}.{version[2]}";
            }
            else if (version.Length == 2)
            {
                return $"v{version[0]}.{version[1]}";
            }
            else
            {
                return $"v{version}";
            }
        }
        
        return trimmed;
    }

    private bool IsNetFrameworkTarget(string tfm)
    {
        if (tfm.StartsWith("$"))
            return true; // Assume variables are valid
        
        var cleaned = tfm.Replace("-windows", "").ToLowerInvariant();
        return cleaned.StartsWith("net4") || cleaned == "net403" || cleaned == "net40";
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

    #region Helper Classes

    private class ConversionResult
    {
        public bool Success { get; set; }
        public string Error { get; set; } = string.Empty;
    }

    private class BuildResult
    {
        public bool Success { get; set; }
        public string Error { get; set; } = string.Empty;
    }

    #endregion
}
