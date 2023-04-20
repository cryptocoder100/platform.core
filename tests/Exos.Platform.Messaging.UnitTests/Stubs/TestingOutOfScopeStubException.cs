namespace Exos.Platform.Messaging.UnitTests.Stubs
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    [ExcludeFromCodeCoverage]
    public class TestingOutOfScopeStubException : Exception
    {
        public TestingOutOfScopeStubException()
        {
        }

        public TestingOutOfScopeStubException(string message) : base(message)
        {
        }

        public TestingOutOfScopeStubException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
