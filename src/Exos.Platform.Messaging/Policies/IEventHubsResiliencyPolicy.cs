namespace Exos.Platform.Messaging.Policies
{
    using Exos.Platform.AspNetCore.Resiliency.Policies;

    /// <summary>
    /// Resiliency Policy for Event Hub.
    /// </summary>
    public interface IEventHubsResiliencyPolicy : IResilientPolicy
    {
    }
}