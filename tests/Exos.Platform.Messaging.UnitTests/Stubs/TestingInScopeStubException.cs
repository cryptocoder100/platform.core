namespace Exos.Platform.Messaging.UnitTests.Stubs
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    [ExcludeFromCodeCoverage]
    public class TestingInScopeStubException : Exception
    {
        public TestingInScopeStubException(string message) : base(message)
        {
        }

        public TestingInScopeStubException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public TestingInScopeStubException()
        {
        }
    }
}
