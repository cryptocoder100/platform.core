namespace Exos.Platform.IntegrationTesting
{
    /// <summary>
    /// Set the priority of the testcase.
    /// A numeric value.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class TestPriorityAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestPriorityAttribute"/> class.
        /// </summary>
        /// <param name="priority">The priority.</param>
        public TestPriorityAttribute(int priority) => Priority = priority;

        /// <summary>
        /// Gets the priority.
        /// </summary>
        public int Priority { get; private set; }
    }
}

// References:
// https://learn.microsoft.com/en-us/dotnet/core/testing/order-unit-tests?pivots=xunit
// https://github.com/dotnet/samples/tree/main/csharp/unit-testing/XUnit.TestProject