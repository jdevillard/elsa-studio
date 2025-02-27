namespace Elsa.Studio.Workflows.Domain.Contracts;

/// <summary>
/// Generates unique activity IDs.
/// </summary>
public interface IActivityIdGenerator
{
    /// <summary>
    /// Generates a unique activity ID.
    /// </summary>
    string GenerateId();
}