using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;
using Exos.Platform.Messaging.Core.Listener;
using Exos.Platform.Messaging.Repository;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;

namespace Exos.Platform.Messaging.UnitTests.Core
{
    [ExcludeFromCodeCoverage]
    public class FakeMessageConsumer : MessageConsumer
    {
        public FakeMessageConsumer(IMessagingRepository repository, string environment, ILogger logger) : base(repository, environment, logger)
        {
            PublicExceptionReceivedHandler = ExceptionReceivedHandler;
        }

        public Func<ExceptionReceivedEventArgs, Task> PublicExceptionReceivedHandler { get; }
    }
}
