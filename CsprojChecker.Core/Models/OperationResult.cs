namespace CsprojChecker.Core.Models;

/// <summary>
/// Result of a framework operation (change or append)
/// </summary>
public class OperationResult
{
    /// <summary>
    /// Indicates whether the operation was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Error message if the operation failed
    /// </summary>
    public string Error { get; set; } = string.Empty;
    
    /// <summary>
    /// The resulting target framework(s) after the operation
    /// </summary>
    public string? ResultingTargetFramework { get; set; }
}
