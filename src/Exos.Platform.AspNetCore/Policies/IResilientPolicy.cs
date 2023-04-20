namespace Exos.Platform.AspNetCore.Resiliency.Policies
{
    using Polly;

    /// <summary>
    /// Interface for resilient policy.
    /// </summary>
    public interface IResilientPolicy
    {
        /// <summary>
        /// Gets the policy instance..
        /// </summary>
        IsPolicy ResilientPolicy { get; }
    }
}
