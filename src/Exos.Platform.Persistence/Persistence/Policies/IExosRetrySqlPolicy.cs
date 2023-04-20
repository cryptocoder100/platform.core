namespace Exos.Platform.Persistence.Policies
{
    using Exos.Platform.AspNetCore.Resiliency.Policies;

    /// <summary>
    /// Implement Retry Policy for Sql.
    /// </summary>
    public interface IExosRetrySqlPolicy : IResilientPolicy
    {
    }
}