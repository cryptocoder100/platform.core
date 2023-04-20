namespace Exos.Platform.Messaging.Policies
{
    using Exos.Platform.AspNetCore.Resiliency.Policies;

    /// <summary>
    /// Service Bus Resiliency Policy.
    /// </summary>
    public interface IServiceBusResiliencyPolicy : IResilientPolicy
    {
    }
}