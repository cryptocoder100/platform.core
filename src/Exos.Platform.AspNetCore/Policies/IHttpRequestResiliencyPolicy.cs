namespace Exos.Platform.AspNetCore.Resiliency.Policies
{
    /// <summary>
    /// Implements resiliency policy for http requests.
    /// </summary>
    public interface IHttpRequestResiliencyPolicy : IResilientPolicy
    {
    }
}
