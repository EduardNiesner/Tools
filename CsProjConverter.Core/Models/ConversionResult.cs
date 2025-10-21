namespace CsProjConverter.Core.Models;

/// <summary>
/// Result of a project conversion operation
/// </summary>
public class ConversionResult
{
    /// <summary>
    /// Indicates whether the conversion was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Error message if the conversion failed
    /// </summary>
    public string Error { get; set; } = string.Empty;
    
    /// <summary>
    /// The resulting target framework(s) after conversion
    /// </summary>
    public string? ResultingTargetFramework { get; set; }
}
