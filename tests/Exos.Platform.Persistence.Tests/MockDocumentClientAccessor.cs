using System;
using System.Collections.Generic;
using System.Text;
using Exos.Platform.TenancyHelper.PersistenceService;
using Microsoft.Azure.Documents.Client;

namespace Exos.Platform.Persistence.Tests
{
    public class MockDocumentClientAccessor : IDocumentClientAccessor
    {
#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
        public DocumentClient DocumentClient => throw new NotImplementedException();
#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations

        public RepositoryOptions RepositoryOptions => new RepositoryOptions();
    }
}
