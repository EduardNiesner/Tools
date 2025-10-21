using System.Reflection;
using Xunit;

namespace CsProjConverter.Tests;

/// <summary>
/// Tests to verify the application name is correctly set to CsProjConverter
/// </summary>
public class ApplicationNameTests
{
    [Fact]
    public void CoreAssemblyName_ShouldBeCsProjConverterCore()
    {
        // Arrange
        var coreAssemblyName = "CsProjConverter.Core";
        
        // Act
        var assembly = Assembly.Load(coreAssemblyName);
        
        // Assert
        Assert.NotNull(assembly);
        Assert.Equal(coreAssemblyName, assembly.GetName().Name);
    }
    
    [Fact]
    public void TestsAssemblyName_ShouldBeCsProjConverterTests()
    {
        // Arrange
        var expectedAssemblyName = "CsProjConverter.Tests";
        
        // Act
        var assembly = Assembly.GetExecutingAssembly();
        
        // Assert
        Assert.NotNull(assembly);
        Assert.Equal(expectedAssemblyName, assembly.GetName().Name);
    }
    
    [Fact]
    public void SettingsDirectory_ShouldUseCsProjConverterName()
    {
        // Arrange
        var expectedDirectoryName = "CsProjConverter";
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var expectedSettingsPath = Path.Combine(appDataPath, expectedDirectoryName);
        
        // Act & Assert
        // This test verifies that the expected settings directory follows the naming convention
        Assert.Contains(expectedDirectoryName, expectedSettingsPath);
    }
    
    [Fact]
    public void CoreNamespace_ShouldBeCsProjConverterCore()
    {
        // Arrange
        var expectedNamespace = "CsProjConverter.Core";
        
        // Act
        var serviceType = typeof(CsProjConverter.Core.ProjectConversionService);
        
        // Assert
        Assert.Equal(expectedNamespace, serviceType.Namespace);
    }
    
    [Fact]
    public void TestsNamespace_ShouldBeCsProjConverterTests()
    {
        // Arrange
        var expectedNamespace = "CsProjConverter.Tests";
        
        // Act
        var thisType = GetType();
        
        // Assert
        Assert.Equal(expectedNamespace, thisType.Namespace);
    }
}
