#pragma warning disable SA1402 // FileMayOnlyContainASingleType
#pragma warning disable CA1001 // Types that own disposable fields should be disposable
namespace Exos.Platform.TenancyHelper.PersistenceService
{
    using System;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json;

    /// <inheritdoc/>
    public class DocumentClientAccessor : IDocumentClientAccessor
    {
        private readonly DocumentClient _documentClient;
        private readonly RepositoryOptions _repositoryOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentClientAccessor"/> class.
        /// </summary>
        /// <param name="repositoryOptions">RepositoryOptions.</param>
        public DocumentClientAccessor(IOptions<RepositoryOptions> repositoryOptions)
        {
            if (repositoryOptions == null)
            {
                throw new ArgumentNullException(nameof(repositoryOptions));
            }

            _repositoryOptions = repositoryOptions.Value ?? throw new ArgumentNullException(nameof(repositoryOptions));

            if (!Enum.TryParse<ConnectionMode>(_repositoryOptions.ConnectionMode, out ConnectionMode connMode))
            {
                connMode = ConnectionMode.Gateway;
            }

            var connPolicy = new ConnectionPolicy
            {
                ConnectionMode = connMode,
                ConnectionProtocol = Protocol.Tcp,
                MaxConnectionLimit = 1000,
                RetryOptions = new RetryOptions
                {
                    MaxRetryAttemptsOnThrottledRequests = 10,
                    MaxRetryWaitTimeInSeconds = 60,
                },
            };

            ConsistencyLevel? consistencyLevel = null;
            if (!string.IsNullOrEmpty(_repositoryOptions.ConsistencyLevel))
            {
                switch (_repositoryOptions.ConsistencyLevel)
                {
                    case "Strong":
                        consistencyLevel = ConsistencyLevel.Strong;
                        break;
                    case "BoundedStaleness":
                        consistencyLevel = ConsistencyLevel.BoundedStaleness;
                        break;
                    case "Session":
                        consistencyLevel = ConsistencyLevel.Session;
                        break;
                    case "Eventual":
                        consistencyLevel = ConsistencyLevel.Eventual;
                        break;
                    case "ConsistentPrefix":
                        consistencyLevel = ConsistencyLevel.ConsistentPrefix;
                        break;
                    default:
                        break;
                }
            }

            _documentClient = new DocumentClient(
                _repositoryOptions.Endpoint,
                _repositoryOptions.AuthKey,
                JsonConvert.DefaultSettings.Invoke(),
                connPolicy,
                consistencyLevel);
        }

        /// <inheritdoc/>
        public DocumentClient DocumentClient
        {
            get { return _documentClient; }
        }

        /// <inheritdoc/>
        public RepositoryOptions RepositoryOptions
        {
            get { return _repositoryOptions; }
        }
    }

    /// <inheritdoc cref="DocumentClientAccessor"/>
    public class DocumentClientAccessor1 : DocumentClientAccessor, IDocumentClientAccessor1
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentClientAccessor1"/> class.
        /// </summary>
        /// <param name="options">RepositoryOptions.</param>
        public DocumentClientAccessor1(IOptions<RepositoryOptionsInstance1> options) : base(options)
        {
        }
    }

    /// <inheritdoc cref="DocumentClientAccessor"/>
    public class DocumentClientAccessor2 : DocumentClientAccessor, IDocumentClientAccessor2
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentClientAccessor2"/> class.
        /// </summary>
        /// <param name="options">RepositoryOptions.</param>
        public DocumentClientAccessor2(IOptions<RepositoryOptionsInstance2> options) : base(options)
        {
        }
    }

    /// <inheritdoc cref="DocumentClientAccessor"/>
    public class DocumentClientAccessor3 : DocumentClientAccessor, IDocumentClientAccessor3
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentClientAccessor3"/> class.
        /// </summary>
        /// <param name="options">RepositoryOptions.</param>
        public DocumentClientAccessor3(IOptions<RepositoryOptionsInstance3> options) : base(options)
        {
        }
    }

    /// <inheritdoc cref="DocumentClientAccessor"/>
    public class DocumentClientAccessor4 : DocumentClientAccessor, IDocumentClientAccessor4
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentClientAccessor4"/> class.
        /// </summary>
        /// <param name="options">RepositoryOptions.</param>
        public DocumentClientAccessor4(IOptions<RepositoryOptionsInstance4> options) : base(options)
        {
        }
    }

    /// <inheritdoc cref="DocumentClientAccessor"/>
    public class DocumentClientAccessor5 : DocumentClientAccessor, IDocumentClientAccessor5
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentClientAccessor5"/> class.
        /// </summary>
        /// <param name="options">RepositoryOptions.</param>
        public DocumentClientAccessor5(IOptions<RepositoryOptionsInstance5> options) : base(options)
        {
        }
    }

    /// <inheritdoc cref="DocumentClientAccessor"/>
    public class DocumentClientAccessor6 : DocumentClientAccessor, IDocumentClientAccessor6
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentClientAccessor6"/> class.
        /// </summary>
        /// <param name="options">RepositoryOptions.</param>
        public DocumentClientAccessor6(IOptions<RepositoryOptionsInstance6> options) : base(options)
        {
        }
    }
}
#pragma warning restore SA1402 // FileMayOnlyContainASingleType
#pragma warning restore CA1001 // Types that own disposable fields should be disposable